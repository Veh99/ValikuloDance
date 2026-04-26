using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ValikuloDance.Application.DTOs.Booking;
using ValikuloDance.Application.Interfaces;
using ValikuloDance.Domain.Entities;
using ValikuloDance.Infrastructure.Data;

namespace ValikuloDance.Application.Services
{
    public class BookingService
    {
        private static readonly IReadOnlyList<TrainerWorkingHour> DefaultWorkingHours = Enumerable
            .Range(0, 7)
            .Select(day => new TrainerWorkingHour
            {
                Id = Guid.Empty,
                TrainerId = Guid.Empty,
                DayOfWeek = (DayOfWeek)day,
                StartTimeLocal = new TimeSpan(9, 0, 0),
                EndTimeLocal = new TimeSpan(21, 0, 0),
                SlotDurationMinutes = 15,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            })
            .ToList();

        private readonly AppDbContext _context;
        private readonly ITelegramService _telegramService;

        public BookingService(AppDbContext context, ITelegramService telegramService)
        {
            _context = context;
            _telegramService = telegramService;
        }

        public async Task<BookingResponse> CreateBookingAsync(CreateBookingRequest request, ClaimsPrincipal userClaims)
        {
            var userId = ExtractUserId(userClaims);
            var user = await _context.Users.FindAsync(userId)
                ?? throw new KeyNotFoundException("Пользователь не найден");

            var trainer = await _context.Trainers
                .Include(t => t.User)
                .Include(t => t.WorkingHours.Where(w => w.IsActive))
                .FirstOrDefaultAsync(t => t.Id == request.TrainerId)
                ?? throw new KeyNotFoundException("Тренер не найден");

            var service = await _context.Services.FindAsync(request.ServiceId)
                ?? throw new KeyNotFoundException("Услуга не найдена");

            if (service.IsPackage)
                throw new InvalidOperationException("Абонементы нельзя бронировать как отдельное занятие. Сначала оформите покупку абонемента.");

            var startTimeUtc = NormalizeToUtc(request.StartTime);
            if (startTimeUtc < DateTime.UtcNow)
                throw new InvalidOperationException("Нельзя создать запись на прошедшее время");

            if (startTimeUtc < DateTime.UtcNow.AddHours(3))
                throw new InvalidOperationException("Запись возможна только не позднее чем за 3 часа до начала занятия");

            var endTimeUtc = startTimeUtc.AddMinutes(service.DurationMinutes);
            await EnsureSlotsGeneratedAsync(trainer, startTimeUtc.Date, startTimeUtc.Date);

            var slotsToBook = await GetBookableSlotsAsync(trainer.Id, startTimeUtc, endTimeUtc);
            if (slotsToBook.Count == 0)
                throw new InvalidOperationException("Выбранное время недоступно");

            var bookingPrice = user.HasLateCancellationPenalty ? service.Price * 2 : service.Price;

            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TrainerId = request.TrainerId,
                ServiceId = request.ServiceId,
                StartTime = startTimeUtc,
                EndTime = endTimeUtc,
                CreatedAt = DateTime.UtcNow,
                Notes = request.Notes,
                Status = "Pending",
                PriceAtBooking = bookingPrice
            };

            _context.Bookings.Add(booking);

            if (user.HasLateCancellationPenalty)
            {
                user.HasLateCancellationPenalty = false;
                user.UpdatedAt = DateTime.UtcNow;
            }

            foreach (var slot in slotsToBook)
            {
                slot.IsBooked = true;
                slot.IsAvailable = false;
                slot.BookingId = booking.Id;
                slot.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            var bookingForNotification = await LoadBookingAsync(booking.Id)
                ?? throw new KeyNotFoundException("Запись не найдена после создания");

            await _telegramService.SendBookingPendingAsync(bookingForNotification);
            return MapBookingResponse(bookingForNotification);
        }

        public async Task<List<AvailableDateResponse>> GetAvailableDatesAsync(Guid trainerId, Guid serviceId, int days = 14)
        {
            if (days < 1)
                days = 1;
            if (days > 30)
                days = 30;

            var trainer = await _context.Trainers
                .Include(t => t.WorkingHours.Where(w => w.IsActive))
                .FirstOrDefaultAsync(t => t.Id == trainerId)
                ?? throw new KeyNotFoundException("Тренер не найден");

            var service = await _context.Services.FindAsync(serviceId)
                ?? throw new KeyNotFoundException("Услуга не найдена");

            var timeZone = GetMoscowTimeZone();
            var todayLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone).Date;
            var endDateLocal = todayLocal.AddDays(days - 1);

            await EnsureSlotsGeneratedAsync(
                trainer,
                TimeZoneInfo.ConvertTimeToUtc(todayLocal, timeZone).Date,
                TimeZoneInfo.ConvertTimeToUtc(endDateLocal, timeZone).Date);

            var result = new List<AvailableDateResponse>();

            for (var localDate = todayLocal; localDate <= endDateLocal; localDate = localDate.AddDays(1))
            {
                var startTimes = await GetAvailableStartTimesForDateAsync(trainer.Id, localDate, service.DurationMinutes);
                if (startTimes.Count == 0)
                    continue;

                result.Add(new AvailableDateResponse
                {
                    Date = DateTime.SpecifyKind(localDate, DateTimeKind.Unspecified),
                    AvailableSlotCount = startTimes.Count
                });
            }

            return result;
        }

        public async Task<List<AvailableSlotResponse>> GetAvailableSlotsAsync(Guid trainerId, Guid serviceId, DateTime date)
        {
            var trainer = await _context.Trainers
                .Include(t => t.WorkingHours.Where(w => w.IsActive))
                .FirstOrDefaultAsync(t => t.Id == trainerId)
                ?? throw new KeyNotFoundException("Тренер не найден");

            var service = await _context.Services.FindAsync(serviceId)
                ?? throw new KeyNotFoundException("Услуга не найдена");

            var timeZone = GetMoscowTimeZone();
            var localDate = date.Kind switch
            {
                DateTimeKind.Utc => TimeZoneInfo.ConvertTimeFromUtc(date, timeZone).Date,
                DateTimeKind.Local => date.ToLocalTime().Date,
                _ => date.Date
            };

            await EnsureSlotsGeneratedAsync(
                trainer,
                TimeZoneInfo.ConvertTimeToUtc(localDate, timeZone).Date,
                TimeZoneInfo.ConvertTimeToUtc(localDate, timeZone).Date);

            var startTimes = await GetAvailableStartTimesForDateAsync(trainer.Id, localDate, service.DurationMinutes);

            return startTimes.Select(startTimeUtc => new AvailableSlotResponse
            {
                StartTime = startTimeUtc,
                EndTime = startTimeUtc.AddMinutes(service.DurationMinutes),
                IsAvailable = true
            }).ToList();
        }

        public async Task<List<BookingResponse>> GetUserBookingsAsync(ClaimsPrincipal userClaims)
        {
            var userId = ExtractUserId(userClaims);

            var bookings = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Trainer).ThenInclude(t => t.User)
                .Include(b => b.Service)
                .Where(b => b.UserId == userId)
                .OrderBy(b => b.StartTime)
                .ToListAsync();

            return bookings.Select(MapBookingResponse).ToList();
        }

        public async Task<List<BookingResponse>> GetTrainerBookingsAsync(ClaimsPrincipal userClaims)
        {
            var trainer = await GetTrainerForCurrentUserAsync(userClaims);

            var bookings = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Trainer).ThenInclude(t => t.User)
                .Include(b => b.Service)
                .Where(b => b.TrainerId == trainer.Id)
                .OrderBy(b => b.StartTime)
                .ToListAsync();

            return bookings.Select(MapBookingResponse).ToList();
        }

        public async Task<BookingResponse> ConfirmBookingAsync(Guid bookingId, ClaimsPrincipal userClaims)
        {
            var trainer = await GetTrainerForCurrentUserAsync(userClaims);

            var booking = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Trainer).ThenInclude(t => t.User)
                .Include(b => b.Service)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.TrainerId == trainer.Id)
                ?? throw new KeyNotFoundException("Запись не найдена");

            if (booking.Status == "Cancelled")
                throw new InvalidOperationException("Нельзя подтвердить отмененную запись");

            if (booking.Status == "Completed")
                throw new InvalidOperationException("Занятие уже завершено");

            booking.Status = "Confirmed";
            booking.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await _telegramService.SendBookingConfirmationAsync(booking);

            return MapBookingResponse(booking);
        }

        public async Task<BookingResponse> CancelBookingByTrainerAsync(Guid bookingId, ClaimsPrincipal userClaims)
        {
            var trainer = await GetTrainerForCurrentUserAsync(userClaims);

            var booking = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Trainer).ThenInclude(t => t.User)
                .Include(b => b.Service)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.TrainerId == trainer.Id)
                ?? throw new KeyNotFoundException("Запись не найдена");

            if (booking.Status == "Cancelled")
                return MapBookingResponse(booking);

            if (booking.Status == "Completed")
                throw new InvalidOperationException("Нельзя отменить завершенное занятие");

            if (HasPenaltyPrice(booking))
                throw new InvalidOperationException("Эту запись нельзя отменить онлайн, потому что к ней уже применен штрафной коэффициент x2. Свяжитесь с администратором.");

            booking.Status = "Cancelled";
            booking.UpdatedAt = DateTime.UtcNow;

            var bookedSlots = await _context.ScheduleSlots
                .Where(s => s.BookingId == booking.Id)
                .ToListAsync();

            foreach (var slot in bookedSlots)
            {
                slot.BookingId = null;
                slot.IsBooked = false;
                slot.IsAvailable = true;
                slot.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            await _telegramService.SendBookingCancellationAsync(booking);

            return MapBookingResponse(booking);
        }

        public async Task<int> CompleteExpiredBookingsAsync()
        {
            var expiredBookings = await _context.Bookings
                .Where(b => b.Status == "Confirmed" && b.EndTime <= DateTime.UtcNow)
                .ToListAsync();

            if (expiredBookings.Count == 0)
                return 0;

            foreach (var booking in expiredBookings)
            {
                booking.Status = "Completed";
                booking.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return expiredBookings.Count;
        }

        public async Task<bool> CancelBookingAsync(Guid bookingId, ClaimsPrincipal userClaims)
        {
            var userId = ExtractUserId(userClaims);
            var booking = await GetBookingAsync(bookingId, userId);

            if (booking == null)
                return false;

            if (booking.Status == "Cancelled")
                return true;

            if (booking.Status == "Completed")
                throw new InvalidOperationException("Нельзя отменить завершенное занятие");

            booking.Status = "Cancelled";
            booking.UpdatedAt = DateTime.UtcNow;

            if (booking.StartTime < DateTime.UtcNow.AddHours(3))
            {
                booking.User.HasLateCancellationPenalty = true;
                booking.User.UpdatedAt = DateTime.UtcNow;
            }

            var bookedSlots = await _context.ScheduleSlots
                .Where(s => s.BookingId == booking.Id)
                .ToListAsync();

            foreach (var slot in bookedSlots)
            {
                slot.BookingId = null;
                slot.IsBooked = false;
                slot.IsAvailable = true;
                slot.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            await _telegramService.SendBookingCancellationAsync(booking);
            return true;
        }

        private async Task<List<DateTime>> GetAvailableStartTimesForDateAsync(Guid trainerId, DateTime localDate, int durationMinutes)
        {
            var timeZone = GetMoscowTimeZone();
            var dayStartUtc = TimeZoneInfo.ConvertTimeToUtc(localDate, timeZone);
            var nextDayStartUtc = TimeZoneInfo.ConvertTimeToUtc(localDate.AddDays(1), timeZone);
            var bookingDeadlineUtc = DateTime.UtcNow.AddHours(3);

            var slots = await _context.ScheduleSlots
                .Where(s => s.TrainerId == trainerId && s.StartTime >= dayStartUtc && s.StartTime < nextDayStartUtc)
                .OrderBy(s => s.StartTime)
                .ToListAsync();

            if (slots.Count == 0)
                return new List<DateTime>();

            var availableStarts = new List<DateTime>();

            foreach (var slot in slots.Where(IsSlotFree))
            {
                if (slot.StartTime < bookingDeadlineUtc)
                    continue;

                var requestedEnd = slot.StartTime.AddMinutes(durationMinutes);
                var candidateSlots = slots
                    .Where(s => s.StartTime >= slot.StartTime && s.StartTime < requestedEnd)
                    .OrderBy(s => s.StartTime)
                    .ToList();

                if (CoversWholeRange(candidateSlots, slot.StartTime, requestedEnd))
                {
                    availableStarts.Add(slot.StartTime);
                }
            }

            return availableStarts;
        }

        private async Task<List<ScheduleSlot>> GetBookableSlotsAsync(Guid trainerId, DateTime startTimeUtc, DateTime endTimeUtc)
        {
            var slots = await _context.ScheduleSlots
                .Where(s => s.TrainerId == trainerId && s.StartTime < endTimeUtc && s.EndTime > startTimeUtc)
                .OrderBy(s => s.StartTime)
                .ToListAsync();

            if (!CoversWholeRange(slots, startTimeUtc, endTimeUtc))
                return new List<ScheduleSlot>();

            return slots;
        }

        private static bool CoversWholeRange(List<ScheduleSlot> slots, DateTime requestedStart, DateTime requestedEnd)
        {
            if (slots.Count == 0)
                return false;

            if (slots.Any(s => !IsSlotFree(s)))
                return false;

            if (slots.First().StartTime != requestedStart)
                return false;

            var cursor = requestedStart;
            foreach (var slot in slots)
            {
                if (slot.StartTime != cursor)
                    return false;

                cursor = slot.EndTime;
            }

            return cursor >= requestedEnd;
        }

        private async Task EnsureSlotsGeneratedAsync(Trainer trainer, DateTime utcStartDate, DateTime utcEndDate)
        {
            var timeZone = GetMoscowTimeZone();
            var localStartDate = TimeZoneInfo.ConvertTimeFromUtc(utcStartDate, timeZone).Date;
            var localEndDate = TimeZoneInfo.ConvertTimeFromUtc(utcEndDate, timeZone).Date;

            var workingHours = trainer.WorkingHours.Where(w => w.IsActive).ToList();
            if (workingHours.Count == 0)
                workingHours = DefaultWorkingHours.ToList();

            var generationWindowStartUtc = TimeZoneInfo.ConvertTimeToUtc(localStartDate, timeZone);
            var generationWindowEndUtc = TimeZoneInfo.ConvertTimeToUtc(localEndDate.AddDays(1), timeZone);

            var existingSlotStarts = await _context.ScheduleSlots
                .Where(s =>
                    s.TrainerId == trainer.Id &&
                    s.StartTime >= generationWindowStartUtc &&
                    s.StartTime < generationWindowEndUtc)
                .Select(s => s.StartTime)
                .ToListAsync();

            var existingStartSet = existingSlotStarts.ToHashSet();
            var slotsAdded = false;

            for (var localDate = localStartDate; localDate <= localEndDate; localDate = localDate.AddDays(1))
            {
                var hoursForDay = workingHours
                    .Where(w => w.DayOfWeek == localDate.DayOfWeek)
                    .ToList();

                foreach (var hours in hoursForDay)
                {
                    var localCursor = localDate.Add(hours.StartTimeLocal);
                    var localEnd = localDate.Add(hours.EndTimeLocal);

                    while (localCursor < localEnd)
                    {
                        var slotEndLocal = localCursor.AddMinutes(hours.SlotDurationMinutes);
                        if (slotEndLocal > localEnd)
                            break;

                        var slotStartUtc = TimeZoneInfo.ConvertTimeToUtc(
                            DateTime.SpecifyKind(localCursor, DateTimeKind.Unspecified),
                            timeZone);

                        var slotEndUtc = TimeZoneInfo.ConvertTimeToUtc(
                            DateTime.SpecifyKind(slotEndLocal, DateTimeKind.Unspecified),
                            timeZone);

                        if (!existingStartSet.Contains(slotStartUtc))
                        {
                            _context.ScheduleSlots.Add(new ScheduleSlot
                            {
                                Id = Guid.NewGuid(),
                                TrainerId = trainer.Id,
                                StartTime = slotStartUtc,
                                EndTime = slotEndUtc,
                                IsAvailable = true,
                                IsBooked = false,
                                CreatedAt = DateTime.UtcNow
                            });
                            existingStartSet.Add(slotStartUtc);
                            slotsAdded = true;
                        }

                        localCursor = slotEndLocal;
                    }
                }
            }

            if (slotsAdded)
                await _context.SaveChangesAsync();
        }

        private async Task<Booking?> GetBookingAsync(Guid bookingId, Guid userId)
        {
            return await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Trainer).ThenInclude(t => t.User)
                .Include(b => b.Service)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == userId);
        }

        private async Task<Booking?> LoadBookingAsync(Guid bookingId)
        {
            return await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Trainer).ThenInclude(t => t.User)
                .Include(b => b.Service)
                .FirstOrDefaultAsync(b => b.Id == bookingId);
        }

        private async Task<Trainer> GetTrainerForCurrentUserAsync(ClaimsPrincipal userClaims)
        {
            var userId = ExtractUserId(userClaims);

            return await _context.Trainers
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.UserId == userId && t.IsActive)
                ?? throw new UnauthorizedAccessException("Только тренер может выполнить это действие");
        }

        private static BookingResponse MapBookingResponse(Booking booking)
        {
            return new BookingResponse
            {
                Id = booking.Id,
                UserId = booking.UserId,
                TrainerId = booking.TrainerId,
                ServiceId = booking.ServiceId,
                UserName = booking.User.Name,
                TrainerName = booking.Trainer.User.Name,
                ServiceName = booking.Service.Name,
                StartTime = booking.StartTime,
                EndTime = booking.EndTime,
                Status = booking.Status,
                Price = booking.PriceAtBooking > 0 ? booking.PriceAtBooking : booking.Service.Price,
                HasPenaltyPrice = HasPenaltyPrice(booking),
                CanBeCancelledByUser = CanBeCancelledByUser(booking),
                Notes = booking.Notes
            };
        }

        private static bool HasPenaltyPrice(Booking booking)
        {
            return booking.PriceAtBooking > booking.Service.Price;
        }

        private static bool CanBeCancelledByUser(Booking booking)
        {
            var normalizedStatus = booking.Status?.ToLowerInvariant();
            if (normalizedStatus is not ("pending" or "confirmed"))
                return false;

            return !HasPenaltyPrice(booking);
        }

        private static bool IsSlotFree(ScheduleSlot slot) => slot.IsAvailable && !slot.IsBooked && slot.BookingId == null;

        private static Guid ExtractUserId(ClaimsPrincipal userClaims)
        {
            var userIdClaim = userClaims.FindFirst("sub")?.Value
               ?? userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? userClaims.FindFirst("id")?.Value
               ?? userClaims.FindFirst("userId")?.Value
               ?? userClaims.FindFirst(ClaimTypes.PrimarySid)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
                throw new UnauthorizedAccessException("Пользователь не авторизован");

            if (!Guid.TryParse(userIdClaim, out var userId))
                throw new UnauthorizedAccessException("Некорректный идентификатор пользователя");

            return userId;
        }

        private static DateTime NormalizeToUtc(DateTime dateTime)
        {
            return dateTime.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
                : dateTime.ToUniversalTime();
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
