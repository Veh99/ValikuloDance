using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using ValikuloDance.Api.Settings;
using ValikuloDance.Application.DTOs.Booking;
using ValikuloDance.Application.DTOs.Subscription;
using ValikuloDance.Application.Interfaces;
using ValikuloDance.Domain.Entities;
using ValikuloDance.Infrastructure.Data;

namespace ValikuloDance.Application.Services
{
    public class SubscriptionService
    {
        private static readonly DayOfWeek[] AllowedGroupDays = [DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Saturday];

        private readonly AppDbContext _context;
        private readonly ITelegramService _telegramService;
        private readonly SubscriptionWorkflowSettings _settings;

        public SubscriptionService(
            AppDbContext context,
            ITelegramService telegramService,
            IOptions<SubscriptionWorkflowSettings> settings)
        {
            _context = context;
            _telegramService = telegramService;
            _settings = settings.Value;
        }

        public async Task<List<SubscriptionPlanResponse>> GetPlansAsync(string? format = null)
        {
            await EnsureSubscriptionPlansSyncedAsync();

            var query = _context.SubscriptionPlans
                .AsNoTracking()
                .Where(x => x.IsActive);

            if (!string.IsNullOrWhiteSpace(format))
            {
                query = query.Where(x => x.Format == format);
            }

            return await query
                .OrderBy(x => x.Format)
                .ThenBy(x => x.SessionsCount)
                .Select(x => new SubscriptionPlanResponse
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description,
                    Format = x.Format,
                    SessionsCount = x.SessionsCount,
                    ValidityMonths = x.ValidityMonths,
                    Price = x.Price
                })
                .ToListAsync();
        }

        public async Task<SubscriptionResponse> CreateRequestAsync(CreateSubscriptionRequestDto request, ClaimsPrincipal userClaims)
        {
            await EnsureSubscriptionPlansSyncedAsync();

            var userId = ExtractUserId(userClaims);
            var plan = await _context.SubscriptionPlans
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == request.SubscriptionPlanId && x.IsActive)
                ?? throw new KeyNotFoundException("План абонемента не найден.");

            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Id == userId)
                ?? throw new KeyNotFoundException("Пользователь не найден.");

            var hasTelegramBinding = await _context.TelegramChatBindings
                .AnyAsync(x => x.UserId == userId && x.IsActive && !x.IsDeleted);

            if (!hasTelegramBinding)
            {
                throw new InvalidOperationException("Перед оформлением абонемента запустите Telegram-бота и нажмите Start, чтобы получить реквизиты и уведомления.");
            }

            var hasOpenSubscription = await _context.Subscriptions.AnyAsync(x =>
                x.UserId == userId &&
                x.IsActive &&
                (x.Status == "PendingPayment" || x.Status == "Active") &&
                x.SubscriptionPlan.Format == plan.Format);

            if (hasOpenSubscription)
            {
                throw new InvalidOperationException("У вас уже есть активный или ожидающий подтверждения абонемент этого формата.");
            }

            var now = DateTime.UtcNow;
            var subscription = new Subscription
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                SubscriptionPlanId = plan.Id,
                TotalSessions = plan.SessionsCount,
                UsedSessions = 0,
                RequestedAt = now,
                PaymentDeadlineAt = now.AddHours(1),
                Status = "PendingPayment",
                IsActive = true,
                CreatedAt = now
            };

            _context.Subscriptions.Add(subscription);
            await _context.SaveChangesAsync();

            var createdSubscription = await _context.Subscriptions
                .Include(x => x.User)
                .Include(x => x.SubscriptionPlan)
                .FirstAsync(x => x.Id == subscription.Id);

            await _telegramService.SendSubscriptionRequestCreatedAsync(createdSubscription);
            return MapSubscription(createdSubscription);
        }

        public async Task<List<SubscriptionResponse>> GetUserSubscriptionsAsync(ClaimsPrincipal userClaims)
        {
            var userId = ExtractUserId(userClaims);

            var subscriptions = await _context.Subscriptions
                .AsNoTracking()
                .Include(x => x.SubscriptionPlan)
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return subscriptions.Select(MapSubscription).ToList();
        }

        public async Task<List<ActiveSubscriptionOptionResponse>> GetActiveSubscriptionsAsync(ClaimsPrincipal userClaims, string format)
        {
            var userId = ExtractUserId(userClaims);
            var normalizedFormat = NormalizeFormat(format);

            var subscriptions = await _context.Subscriptions
                .AsNoTracking()
                .Include(x => x.SubscriptionPlan)
                .Where(x =>
                    x.UserId == userId &&
                    x.Status == "Active" &&
                    x.IsActive &&
                    x.SubscriptionPlan.Format == normalizedFormat &&
                    x.ExpiresAt > DateTime.UtcNow &&
                    x.TotalSessions > x.UsedSessions)
                .OrderBy(x => x.ExpiresAt)
                .ToListAsync();

            return subscriptions.Select(x => new ActiveSubscriptionOptionResponse
            {
                Id = x.Id,
                PlanName = x.SubscriptionPlan.Name,
                Format = x.SubscriptionPlan.Format,
                RemainingSessions = x.TotalSessions - x.UsedSessions,
                ExpiresAt = x.ExpiresAt
            }).ToList();
        }

        public async Task<Subscription> ValidateSubscriptionForBookingAsync(Guid userId, Guid subscriptionId, string expectedFormat)
        {
            var normalizedFormat = NormalizeFormat(expectedFormat);

            var subscription = await _context.Subscriptions
                .Include(x => x.SubscriptionPlan)
                .FirstOrDefaultAsync(x => x.Id == subscriptionId && x.UserId == userId && x.IsActive)
                ?? throw new InvalidOperationException("Абонемент не найден.");

            if (subscription.Status != "Active")
                throw new InvalidOperationException("Абонемент ещё не активирован и не может быть использован для записи.");

            if (subscription.SubscriptionPlan.Format != normalizedFormat)
                throw new InvalidOperationException("Этот абонемент нельзя использовать для выбранного формата занятия.");

            if (subscription.ExpiresAt <= DateTime.UtcNow)
                throw new InvalidOperationException("Срок действия абонемента истёк.");

            if (subscription.UsedSessions >= subscription.TotalSessions)
                throw new InvalidOperationException("В абонементе не осталось доступных занятий.");

            return subscription;
        }

        public async Task ConsumeSubscriptionSessionAsync(Booking booking)
        {
            if (booking.SubscriptionId == null || booking.PaymentMode != "Subscription" || booking.IsSubscriptionSessionConsumed)
            {
                return;
            }

            var subscription = await _context.Subscriptions
                .Include(x => x.SubscriptionPlan)
                .FirstOrDefaultAsync(x => x.Id == booking.SubscriptionId.Value);

            if (subscription == null || !subscription.IsActive)
            {
                return;
            }

            if (subscription.Status != "Active")
            {
                return;
            }

            subscription.UsedSessions = Math.Min(subscription.TotalSessions, subscription.UsedSessions + 1);
            subscription.UpdatedAt = DateTime.UtcNow;
            booking.IsSubscriptionSessionConsumed = true;
            booking.UpdatedAt = DateTime.UtcNow;

            if (subscription.UsedSessions >= subscription.TotalSessions)
            {
                subscription.Status = "Exhausted";
                subscription.IsActive = false;
            }

            await _context.SaveChangesAsync();
        }

        public async Task RestoreSubscriptionSessionAsync(Booking booking)
        {
            if (booking.SubscriptionId == null || booking.PaymentMode != "Subscription" || !booking.IsSubscriptionSessionConsumed)
            {
                return;
            }

            var subscription = await _context.Subscriptions
                .Include(x => x.SubscriptionPlan)
                .FirstOrDefaultAsync(x => x.Id == booking.SubscriptionId.Value);

            if (subscription == null)
            {
                return;
            }

            subscription.UsedSessions = Math.Max(0, subscription.UsedSessions - 1);
            subscription.UpdatedAt = DateTime.UtcNow;
            booking.IsSubscriptionSessionConsumed = false;
            booking.UpdatedAt = DateTime.UtcNow;

            if (subscription.Status == "Exhausted")
            {
                var expiresAt = subscription.ExpiresAt ?? DateTime.MaxValue;
                if (expiresAt > DateTime.UtcNow)
                {
                    subscription.Status = "Active";
                    subscription.IsActive = true;
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task<int> ExpirePendingSubscriptionsAsync()
        {
            var now = DateTime.UtcNow;
            var pending = await _context.Subscriptions
                .Include(x => x.User)
                .Include(x => x.SubscriptionPlan)
                .Where(x => x.Status == "PendingPayment" && x.PaymentDeadlineAt <= now && x.IsActive)
                .ToListAsync();

            if (pending.Count == 0)
            {
                return 0;
            }

            foreach (var subscription in pending)
            {
                subscription.Status = "Expired";
                subscription.IsActive = false;
                subscription.UpdatedAt = now;
            }

            await _context.SaveChangesAsync();

            foreach (var subscription in pending)
            {
                await _telegramService.SendSubscriptionExpiredAsync(subscription);
            }

            return pending.Count;
        }

        public async Task<int> ExpireActiveSubscriptionsAsync()
        {
            var now = DateTime.UtcNow;
            var expired = await _context.Subscriptions
                .Where(x => x.Status == "Active" && x.ExpiresAt != null && x.ExpiresAt <= now && x.IsActive)
                .ToListAsync();

            if (expired.Count == 0)
            {
                return 0;
            }

            foreach (var subscription in expired)
            {
                subscription.Status = "Expired";
                subscription.IsActive = false;
                subscription.UpdatedAt = now;
            }

            await _context.SaveChangesAsync();
            return expired.Count;
        }

        public async Task<Subscription?> ApproveSubscriptionAsync(Guid subscriptionId)
        {
            var subscription = await _context.Subscriptions
                .Include(x => x.User)
                .Include(x => x.SubscriptionPlan)
                .FirstOrDefaultAsync(x => x.Id == subscriptionId);

            if (subscription == null || subscription.Status != "PendingPayment" || !subscription.IsActive)
            {
                return null;
            }

            var now = DateTime.UtcNow;
            if (subscription.PaymentDeadlineAt <= now)
            {
                subscription.Status = "Expired";
                subscription.IsActive = false;
                subscription.UpdatedAt = now;
                await _context.SaveChangesAsync();
                await _telegramService.SendSubscriptionExpiredAsync(subscription);
                return subscription;
            }

            subscription.Status = "Active";
            subscription.StartsAt = now;
            subscription.ExpiresAt = now.AddMonths(subscription.SubscriptionPlan.ValidityMonths);
            subscription.ApprovedAt = now;
            subscription.UpdatedAt = now;

            await _context.SaveChangesAsync();
            await _telegramService.SendSubscriptionApprovedAsync(subscription);
            return subscription;
        }

        public async Task<Subscription?> RejectSubscriptionAsync(Guid subscriptionId, string reason)
        {
            var subscription = await _context.Subscriptions
                .Include(x => x.User)
                .Include(x => x.SubscriptionPlan)
                .FirstOrDefaultAsync(x => x.Id == subscriptionId);

            if (subscription == null || subscription.Status != "PendingPayment" || !subscription.IsActive)
            {
                return null;
            }

            subscription.Status = "Rejected";
            subscription.IsActive = false;
            subscription.RejectionReason = reason;
            subscription.RejectedAt = DateTime.UtcNow;
            subscription.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await _telegramService.SendSubscriptionRejectedAsync(subscription);
            return subscription;
        }

        public async Task<List<GroupLessonSlotResponse>> GetUpcomingGroupSlotsAsync(Guid serviceId, int days = 21)
        {
            await EnsureSubscriptionPlansSyncedAsync();

            var service = await _context.Services
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == serviceId && x.IsActive)
                ?? throw new KeyNotFoundException("Групповая услуга не найдена.");

            if (service.IsPackage || NormalizeFormat(service.Format) != "Group")
                throw new InvalidOperationException("Для групповой записи нужно выбрать разовое групповое занятие.");

            await EnsureGroupLessonSlotsGeneratedAsync(service, Math.Min(Math.Max(days, 1), _settings.GroupLessonHorizonDays));

            var now = DateTime.UtcNow;
            var slots = await _context.GroupLessonSlots
                .AsNoTracking()
                .Include(x => x.Trainer).ThenInclude(t => t.User)
                .Include(x => x.Service)
                .Where(x => x.ServiceId == serviceId && x.IsActive && x.StartTime >= now)
                .OrderBy(x => x.StartTime)
                .ToListAsync();

            var slotIds = slots.Select(x => x.Id).ToList();
            var occupiedCounts = await _context.Bookings
                .AsNoTracking()
                .Where(x => x.GroupLessonSlotId != null && slotIds.Contains(x.GroupLessonSlotId.Value) && x.Status != "Cancelled")
                .GroupBy(x => x.GroupLessonSlotId!.Value)
                .Select(g => new { SlotId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.SlotId, x => x.Count);

            return slots.Select(slot =>
            {
                occupiedCounts.TryGetValue(slot.Id, out var occupied);
                return new GroupLessonSlotResponse
                {
                    Id = slot.Id,
                    ServiceId = slot.ServiceId,
                    TrainerId = slot.TrainerId,
                    ServiceName = slot.Service.Name,
                    TrainerName = slot.Trainer.User?.Name ?? "Тренер",
                    StartTime = slot.StartTime,
                    EndTime = slot.EndTime,
                    Capacity = slot.Capacity,
                    AvailableSpots = Math.Max(slot.Capacity - occupied, 0)
                };
            }).ToList();
        }

        public async Task<BookingResponse> CreateGroupBookingAsync(CreateGroupBookingRequest request, ClaimsPrincipal userClaims)
        {
            var userId = ExtractUserId(userClaims);
            var user = await _context.Users.FindAsync(userId)
                ?? throw new KeyNotFoundException("Пользователь не найден.");

            var slot = await _context.GroupLessonSlots
                .Include(x => x.Service)
                .Include(x => x.Trainer).ThenInclude(t => t.User)
                .FirstOrDefaultAsync(x => x.Id == request.GroupLessonSlotId && x.IsActive)
                ?? throw new KeyNotFoundException("Групповой слот не найден.");

            if (!AllowedGroupDays.Contains(slot.StartTime.DayOfWeek))
                throw new InvalidOperationException("Для групповых занятий доступны только вторник, четверг и суббота.");

            if (slot.StartTime < DateTime.UtcNow.AddHours(3))
                throw new InvalidOperationException("На групповое занятие можно записаться не позднее чем за 3 часа до начала.");

            var occupiedCount = await _context.Bookings.CountAsync(x =>
                x.GroupLessonSlotId == slot.Id &&
                x.Status != "Cancelled");

            if (occupiedCount >= slot.Capacity)
                throw new InvalidOperationException("На это групповое занятие свободных мест больше нет.");

            var hasExistingBooking = await _context.Bookings.AnyAsync(x =>
                x.UserId == userId &&
                x.GroupLessonSlotId == slot.Id &&
                x.Status != "Cancelled");

            if (hasExistingBooking)
                throw new InvalidOperationException("Вы уже записаны на это групповое занятие.");

            var paymentMode = NormalizePaymentMode(request.PaymentMode);
            Subscription? subscription = null;

            if (paymentMode == "Subscription")
            {
                if (request.SubscriptionId == null)
                    throw new InvalidOperationException("Для записи по абонементу выберите активный абонемент.");

                subscription = await ValidateSubscriptionForBookingAsync(userId, request.SubscriptionId.Value, "Group");
            }

            var price = paymentMode == "Subscription"
                ? 0
                : (user.HasLateCancellationPenalty ? slot.Service.Price * 2 : slot.Service.Price);

            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TrainerId = slot.TrainerId,
                ServiceId = slot.ServiceId,
                SubscriptionId = subscription?.Id,
                GroupLessonSlotId = slot.Id,
                StartTime = slot.StartTime,
                EndTime = slot.EndTime,
                Status = "Pending",
                PaymentMode = paymentMode,
                PriceAtBooking = price,
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow
            };

            _context.Bookings.Add(booking);

            if (user.HasLateCancellationPenalty && paymentMode == "Single")
            {
                user.HasLateCancellationPenalty = false;
                user.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            var bookingForNotification = await _context.Bookings
                .Include(x => x.User)
                .Include(x => x.Trainer).ThenInclude(t => t.User)
                .Include(x => x.Service)
                .Include(x => x.Subscription).ThenInclude(s => s!.SubscriptionPlan)
                .FirstAsync(x => x.Id == booking.Id);

            await _telegramService.SendBookingPendingAsync(bookingForNotification);
            return MapBookingResponse(bookingForNotification);
        }

        public async Task EnsureSubscriptionPlansSyncedAsync()
        {
            var packageServices = await _context.Services
                .Where(x => x.IsActive && x.IsPackage && x.SessionsCount != null)
                .ToListAsync();

            var existingPlans = await _context.SubscriptionPlans
                .Where(x => x.SourceServiceId != null)
                .ToDictionaryAsync(x => x.SourceServiceId!.Value, x => x);

            var changed = false;

            foreach (var service in packageServices)
            {
                var format = NormalizeFormat(service.Format);
                var validityMonths = service.SessionsCount == 20 ? 4 : 2;

                if (existingPlans.TryGetValue(service.Id, out var existing))
                {
                    if (existing.Name != service.Name ||
                        existing.Description != service.Description ||
                        existing.Price != service.Price ||
                        existing.SessionsCount != service.SessionsCount ||
                        existing.ValidityMonths != validityMonths ||
                        existing.Format != format ||
                        !existing.IsActive)
                    {
                        existing.Name = service.Name;
                        existing.Description = service.Description;
                        existing.Price = service.Price;
                        existing.SessionsCount = service.SessionsCount ?? 0;
                        existing.ValidityMonths = validityMonths;
                        existing.Format = format;
                        existing.IsActive = true;
                        existing.UpdatedAt = DateTime.UtcNow;
                        changed = true;
                    }

                    continue;
                }

                _context.SubscriptionPlans.Add(new SubscriptionPlan
                {
                    Id = Guid.NewGuid(),
                    Name = service.Name,
                    Description = service.Description,
                    Format = format,
                    SessionsCount = service.SessionsCount ?? 0,
                    ValidityMonths = validityMonths,
                    Price = service.Price,
                    SourceServiceId = service.Id,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
                changed = true;
            }

            if (changed)
            {
                await _context.SaveChangesAsync();
            }
        }

        private async Task EnsureGroupLessonSlotsGeneratedAsync(Service groupService, int days)
        {
            var trainerId = await ResolveGroupTrainerIdAsync();
            var startTimes = ParseGroupStartTimes();
            var timeZone = GetMoscowTimeZone();
            var todayLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone).Date;
            var lastLocalDate = todayLocal.AddDays(days);

            var existingStarts = await _context.GroupLessonSlots
                .Where(x => x.ServiceId == groupService.Id && x.StartTime >= DateTime.UtcNow.Date)
                .Select(x => x.StartTime)
                .ToListAsync();

            var existingSet = existingStarts.ToHashSet();
            var hasChanges = false;

            for (var date = todayLocal; date <= lastLocalDate; date = date.AddDays(1))
            {
                if (!AllowedGroupDays.Contains(date.DayOfWeek))
                    continue;

                foreach (var startTime in startTimes)
                {
                    var localStart = date.Add(startTime);
                    var localEnd = localStart.AddMinutes(groupService.DurationMinutes);
                    var utcStart = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localStart, DateTimeKind.Unspecified), timeZone);
                    var utcEnd = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localEnd, DateTimeKind.Unspecified), timeZone);

                    if (existingSet.Contains(utcStart))
                        continue;

                    _context.GroupLessonSlots.Add(new GroupLessonSlot
                    {
                        Id = Guid.NewGuid(),
                        ServiceId = groupService.Id,
                        TrainerId = trainerId,
                        StartTime = utcStart,
                        EndTime = utcEnd,
                        Capacity = _settings.GroupLessonCapacity,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    });

                    existingSet.Add(utcStart);
                    hasChanges = true;
                }
            }

            if (hasChanges)
            {
                await _context.SaveChangesAsync();
            }
        }

        private async Task<Guid> ResolveGroupTrainerIdAsync()
        {
            if (_settings.DefaultGroupTrainerId.HasValue)
            {
                var configuredTrainerExists = await _context.Trainers.AnyAsync(x => x.Id == _settings.DefaultGroupTrainerId.Value && x.IsActive);
                if (configuredTrainerExists)
                {
                    return _settings.DefaultGroupTrainerId.Value;
                }
            }

            var trainer = await _context.Trainers
                .OrderBy(x => x.CreatedAt)
                .FirstOrDefaultAsync(x => x.IsActive)
                ?? throw new InvalidOperationException("Для групповых занятий нужен хотя бы один активный тренер.");

            return trainer.Id;
        }

        private List<TimeSpan> ParseGroupStartTimes()
        {
            var values = _settings.GroupLessonStartTimes?.Length > 0
                ? _settings.GroupLessonStartTimes
                : ["19:00"];

            var times = new List<TimeSpan>();
            foreach (var value in values)
            {
                if (TimeSpan.TryParse(value, out var parsed))
                {
                    times.Add(parsed);
                }
            }

            if (times.Count == 0)
            {
                times.Add(new TimeSpan(19, 0, 0));
            }

            return times.Distinct().OrderBy(x => x).ToList();
        }

        private static SubscriptionResponse MapSubscription(Subscription subscription)
        {
            var remaining = Math.Max(subscription.TotalSessions - subscription.UsedSessions, 0);

            return new SubscriptionResponse
            {
                Id = subscription.Id,
                SubscriptionPlanId = subscription.SubscriptionPlanId,
                PlanName = subscription.SubscriptionPlan.Name,
                Format = subscription.SubscriptionPlan.Format,
                TotalSessions = subscription.TotalSessions,
                UsedSessions = subscription.UsedSessions,
                RemainingSessions = remaining,
                ValidityMonths = subscription.SubscriptionPlan.ValidityMonths,
                Price = subscription.SubscriptionPlan.Price,
                Status = subscription.Status,
                RequestedAt = subscription.RequestedAt,
                PaymentDeadlineAt = subscription.PaymentDeadlineAt,
                StartsAt = subscription.StartsAt,
                ExpiresAt = subscription.ExpiresAt,
                RejectionReason = subscription.RejectionReason,
                CanBeUsedForBooking = subscription.Status == "Active" &&
                    subscription.IsActive &&
                    subscription.ExpiresAt > DateTime.UtcNow &&
                    remaining > 0
            };
        }

        private static BookingResponse MapBookingResponse(Booking booking)
        {
            return new BookingResponse
            {
                Id = booking.Id,
                UserId = booking.UserId,
                TrainerId = booking.TrainerId,
                ServiceId = booking.ServiceId,
                SubscriptionId = booking.SubscriptionId,
                GroupLessonSlotId = booking.GroupLessonSlotId,
                UserName = booking.User.Name,
                TrainerName = booking.Trainer.User?.Name ?? "РўСЂРµРЅРµСЂ",
                ServiceName = booking.Service.Name,
                StartTime = booking.StartTime,
                EndTime = booking.EndTime,
                Status = booking.Status,
                Price = booking.PaymentMode == "Subscription" ? 0 : booking.PriceAtBooking,
                HasPenaltyPrice = booking.PriceAtBooking > booking.Service.Price,
                CanBeCancelledByUser = CanBeCancelledByUser(booking),
                PaymentMode = booking.PaymentMode,
                SubscriptionPlanName = booking.Subscription?.SubscriptionPlan?.Name,
                Notes = booking.Notes
            };
        }

        private static bool CanBeCancelledByUser(Booking booking)
        {
            var normalizedStatus = booking.Status?.ToLowerInvariant();
            if (normalizedStatus is not ("pending" or "confirmed"))
                return false;

            return booking.PriceAtBooking <= booking.Service.Price;
        }

        private static string NormalizeFormat(string? format)
        {
            return string.Equals(format, "Group", StringComparison.OrdinalIgnoreCase)
                ? "Group"
                : "Individual";
        }

        private static string NormalizePaymentMode(string? paymentMode)
        {
            return string.Equals(paymentMode, "Subscription", StringComparison.OrdinalIgnoreCase)
                ? "Subscription"
                : "Single";
        }

        private static Guid ExtractUserId(ClaimsPrincipal userClaims)
        {
            var userIdClaim = userClaims.FindFirst("sub")?.Value
               ?? userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? userClaims.FindFirst("id")?.Value
               ?? userClaims.FindFirst("userId")?.Value
               ?? userClaims.FindFirst(ClaimTypes.PrimarySid)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
                throw new UnauthorizedAccessException("Пользователь не авторизован.");

            if (!Guid.TryParse(userIdClaim, out var userId))
                throw new UnauthorizedAccessException("Некорректный идентификатор пользователя.");

            return userId;
        }

        private static TimeZoneInfo GetMoscowTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow");
            }
        }
    }
}
