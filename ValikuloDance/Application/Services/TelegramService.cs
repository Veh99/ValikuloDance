using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using ValikuloDance.Api.Settings;
using ValikuloDance.Application.Interfaces;
using ValikuloDance.Domain.Entities;
using ValikuloDance.Infrastructure.Data;

namespace ValikuloDance.Application.Services
{
    public class TelegramService : ITelegramService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly ILogger<TelegramService> _logger;
        private readonly AppDbContext _context;
        private readonly IServiceProvider _serviceProvider;
        private readonly SubscriptionWorkflowSettings _subscriptionSettings;

        public TelegramService(
            IConfiguration configuration,
            ILogger<TelegramService> logger,
            AppDbContext context,
            IServiceProvider serviceProvider,
            IOptions<SubscriptionWorkflowSettings> subscriptionSettings)
        {
            _logger = logger;
            _context = context;
            _serviceProvider = serviceProvider;
            _subscriptionSettings = subscriptionSettings.Value;
            var botToken = configuration["Telegram:BotToken"]!;
            _botClient = new TelegramBotClient(botToken);
        }

        public async Task HandleUpdateAsync(Update update)
        {
            if (update.CallbackQuery != null)
            {
                await HandleCallbackQueryAsync(update.CallbackQuery);
                return;
            }

            var message = update.Message;
            if (message?.Chat == null)
                return;

            var chatId = message.Chat.Id.ToString();
            var telegramUsername = NormalizeUsername(message.From?.Username ?? message.Chat.Username);
            var text = message.Text?.Trim();

            if (string.IsNullOrWhiteSpace(telegramUsername))
            {
                await _botClient.SendMessage(
                    chatId: new ChatId(message.Chat.Id),
                    text: "Не удалось определить ваш Telegram username. Добавьте username в настройках Telegram и повторите /start.");
                return;
            }

            var user = await FindUserByTelegramUsernameAsync(telegramUsername);
            if (user == null)
            {
                await _botClient.SendMessage(
                    chatId: new ChatId(message.Chat.Id),
                    text: $"Пользователь с username @{telegramUsername} не найден. Укажите этот username в профиле на сайте и снова отправьте /start.");
                return;
            }

            await UpsertChatBindingAsync(user.Id, chatId, telegramUsername);

            if (text != null && text.StartsWith("/start", StringComparison.OrdinalIgnoreCase))
            {
                await _botClient.SendMessage(
                    chatId: new ChatId(message.Chat.Id),
                    text: "Telegram успешно привязан. Теперь вы будете получать уведомления о записях и заявках на абонементы.");
            }
        }

        public async Task SendBookingPendingAsync(Booking booking)
        {
            try
            {
                if (booking?.User == null)
                    return;

                var localStartTime = ConvertToMoscowTime(booking.StartTime);
                var dateStr = localStartTime.ToString("dd.MM.yyyy");
                var timeStr = localStartTime.ToString("HH:mm");
                var trainerName = booking.Trainer?.User?.Name ?? "Тренер";
                var serviceName = booking.Service?.Name ?? "Услуга";
                var servicePrice = GetBookingPriceText(booking);
                var paymentLabel = booking.PaymentMode == "Subscription"
                    ? $"Абонемент{(booking.Subscription?.SubscriptionPlan?.Name != null ? $": {booking.Subscription.SubscriptionPlan.Name}" : string.Empty)}"
                    : "Разово";

                var userRecipient = await ResolveRecipientAsync(
                    booking.User.Id,
                    booking.User.TelegramChatId,
                    booking.User.TelegramUsername);

                if (userRecipient != null)
                {
                    var userMessage = $"""
                    Новая запись

                    Ваша запись создана и ожидает подтверждения тренером:
                    Дата: {dateStr}
                    Время: {timeStr}
                    Услуга: {serviceName}
                    Тренер: {trainerName}
                    Оплата: {paymentLabel}
                    Стоимость: {servicePrice}
                    Статус: Ожидает подтверждения
                    """;

                    await SendMessageAsync(userRecipient, userMessage);
                }

                if (booking.Trainer?.User != null)
                {
                    var trainerRecipient = await ResolveRecipientAsync(
                        booking.Trainer.User.Id,
                        booking.Trainer.User.TelegramChatId,
                        booking.Trainer.User.TelegramUsername);

                    if (trainerRecipient != null)
                    {
                        var trainerMessage = $"""
                        Новая заявка на занятие

                        Клиент: {booking.User.Name}
                        Дата: {dateStr}
                        Время: {timeStr}
                        Услуга: {serviceName}
                        Оплата: {paymentLabel}
                        Статус: Ожидает вашего решения
                        """;

                        var keyboard = new InlineKeyboardMarkup(new[]
                        {
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("Подтвердить", $"booking:confirm:{booking.Id}"),
                                InlineKeyboardButton.WithCallbackData("Отменить", $"booking:cancel:{booking.Id}")
                            }
                        });

                        await SendMessageAsync(trainerRecipient, trainerMessage, keyboard);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отправке уведомления о новой заявке {BookingId}", booking?.Id);
            }
        }

        public async Task SendBookingConfirmationAsync(Booking booking)
        {
            try
            {
                if (booking?.User == null)
                    return;

                var localStartTime = ConvertToMoscowTime(booking.StartTime);
                var dateStr = localStartTime.ToString("dd.MM.yyyy");
                var timeStr = localStartTime.ToString("HH:mm");
                var trainerName = booking.Trainer?.User?.Name ?? "Тренер";
                var serviceName = booking.Service?.Name ?? "Услуга";
                var servicePrice = GetBookingPriceText(booking);

                var userRecipient = await ResolveRecipientAsync(
                    booking.User.Id,
                    booking.User.TelegramChatId,
                    booking.User.TelegramUsername);

                if (userRecipient != null)
                {
                    var userMessage = $"""
                    Запись подтверждена

                    Ваше занятие подтверждено:
                    Дата: {dateStr}
                    Время: {timeStr}
                    Услуга: {serviceName}
                    Тренер: {trainerName}
                    Стоимость: {servicePrice}
                    Статус: Подтверждено
                    """;

                    await SendMessageAsync(userRecipient, userMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отправке подтверждения записи {BookingId}", booking?.Id);
            }
        }

        public async Task SendBookingReminderAsync(Booking booking)
        {
            try
            {
                if (booking?.User == null)
                    return;

                var recipient = await ResolveRecipientAsync(
                    booking.User.Id,
                    booking.User.TelegramChatId,
                    booking.User.TelegramUsername);

                if (recipient == null)
                    return;

                var localStartTime = ConvertToMoscowTime(booking.StartTime);
                var message = $"""
                Напоминание о занятии

                Сегодня в {localStartTime:HH:mm} у вас занятие {booking.Service?.Name} с тренером {booking.Trainer?.User?.Name}.
                """;

                await SendMessageAsync(recipient, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отправке напоминания");
            }
        }

        public async Task SendBookingCancellationAsync(Booking booking)
        {
            try
            {
                if (booking?.User == null)
                    return;

                var userRecipient = await ResolveRecipientAsync(
                    booking.User.Id,
                    booking.User.TelegramChatId,
                    booking.User.TelegramUsername);

                var localStartTime = ConvertToMoscowTime(booking.StartTime);

                if (userRecipient != null)
                {
                    var userMessage = $"""
                    Запись отменена

                    Занятие {booking.Service?.Name} на {localStartTime:dd.MM.yyyy HH:mm} отменено.
                    Статус: Отменено
                    """;

                    await SendMessageAsync(userRecipient, userMessage);
                }

                if (booking.Trainer?.User != null)
                {
                    var trainerRecipient = await ResolveRecipientAsync(
                        booking.Trainer.User.Id,
                        booking.Trainer.User.TelegramChatId,
                        booking.Trainer.User.TelegramUsername);

                    if (trainerRecipient != null)
                    {
                        var trainerMessage = $"""
                        Запись отменена

                        Клиент {booking.User.Name} отменил занятие {booking.Service?.Name} на {localStartTime:dd.MM.yyyy HH:mm}.
                        """;

                        await SendMessageAsync(trainerRecipient, trainerMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отправке отмены записи");
            }
        }

        public async Task SendSubscriptionRequestCreatedAsync(Subscription subscription)
        {
            var userRecipient = await ResolveRecipientAsync(
                subscription.User.Id,
                subscription.User.TelegramChatId,
                subscription.User.TelegramUsername);

            if (userRecipient != null)
            {
                var paymentDetails = BuildPaymentDetailsMessage();
                var userMessage = $"""
                Заявка на абонемент создана

                План: {subscription.SubscriptionPlan.Name}
                Формат: {GetFormatLabel(subscription.SubscriptionPlan.Format)}
                Занятий: {subscription.TotalSessions}
                Стоимость: {subscription.SubscriptionPlan.Price:0.##} ₽
                Оплатите абонемент в течение 1 часа.
                Если оплата и подтверждение не будут завершены за это время, заявка отменится автоматически.

                {paymentDetails}
                """;

                await SendMessageAsync(userRecipient, userMessage);
            }

            var approverRecipient = await ResolveApproverRecipientAsync();
            if (approverRecipient != null)
            {
                var deadline = ConvertToMoscowTime(subscription.PaymentDeadlineAt);
                var approverMessage = $"""
                Новая заявка на абонемент

                Клиент: {subscription.User.Name}
                План: {subscription.SubscriptionPlan.Name}
                Формат: {GetFormatLabel(subscription.SubscriptionPlan.Format)}
                Занятий: {subscription.TotalSessions}
                Стоимость: {subscription.SubscriptionPlan.Price:0.##} ₽
                Оплатить и подтвердить нужно до: {deadline:dd.MM.yyyy HH:mm}
                """;

                var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Подтвердить абонемент", $"subscription:approve:{subscription.Id}"),
                        InlineKeyboardButton.WithCallbackData("Отклонить", $"subscription:reject:{subscription.Id}")
                    }
                });

                await SendMessageAsync(approverRecipient, approverMessage, keyboard);
            }
        }

        public async Task SendSubscriptionApprovedAsync(Subscription subscription)
        {
            var recipient = await ResolveRecipientAsync(
                subscription.User.Id,
                subscription.User.TelegramChatId,
                subscription.User.TelegramUsername);

            if (recipient == null)
                return;

            var expiresAtLocal = subscription.ExpiresAt.HasValue
                ? ConvertToMoscowTime(subscription.ExpiresAt.Value).ToString("dd.MM.yyyy HH:mm")
                : "не указан";

            var message = $"""
            Абонемент активирован

            План: {subscription.SubscriptionPlan.Name}
            Формат: {GetFormatLabel(subscription.SubscriptionPlan.Format)}
            Осталось занятий: {subscription.TotalSessions - subscription.UsedSessions} из {subscription.TotalSessions}
            Действует до: {expiresAtLocal}
            """;

            await SendMessageAsync(recipient, message);
        }

        public async Task SendSubscriptionRejectedAsync(Subscription subscription)
        {
            var recipient = await ResolveRecipientAsync(
                subscription.User.Id,
                subscription.User.TelegramChatId,
                subscription.User.TelegramUsername);

            if (recipient == null)
                return;

            var message = $"""
            Заявка на абонемент отклонена

            План: {subscription.SubscriptionPlan.Name}
            Причина: {subscription.RejectionReason ?? "Не указана"}
            """;

            await SendMessageAsync(recipient, message);
        }

        public async Task SendSubscriptionExpiredAsync(Subscription subscription)
        {
            var recipient = await ResolveRecipientAsync(
                subscription.User.Id,
                subscription.User.TelegramChatId,
                subscription.User.TelegramUsername);

            if (recipient == null)
                return;

            var message = $"""
            Заявка на абонемент истекла

            План: {subscription.SubscriptionPlan.Name}
            В течение 1 часа не было завершено подтверждение оплаты, поэтому заявка автоматически закрыта.
            Если абонемент всё ещё нужен, оформите новую заявку на сайте.
            """;

            await SendMessageAsync(recipient, message);
        }

        public async Task UpsertChatBindingAsync(Guid userId, string telegramChatId, string? telegramUsername)
        {
            var normalizedChatId = telegramChatId.Trim();
            var normalizedUsername = NormalizeUsername(telegramUsername);

            var binding = await _context.TelegramChatBindings
                .FirstOrDefaultAsync(x => x.UserId == userId && !x.IsDeleted);

            if (binding == null)
            {
                binding = new TelegramChatBinding
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    TelegramChatId = normalizedChatId,
                    TelegramUsername = normalizedUsername,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    LastVerifiedAt = DateTime.UtcNow
                };

                _context.TelegramChatBindings.Add(binding);
            }
            else
            {
                binding.TelegramChatId = normalizedChatId;
                binding.TelegramUsername = normalizedUsername;
                binding.IsActive = true;
                binding.IsDeleted = false;
                binding.LastVerifiedAt = DateTime.UtcNow;
                binding.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        private async Task HandleCallbackQueryAsync(CallbackQuery callbackQuery)
        {
            if (string.IsNullOrWhiteSpace(callbackQuery.Data))
                return;

            var parts = callbackQuery.Data.Split(':', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3 || !Guid.TryParse(parts[2], out var entityId))
                return;

            if (parts[0] == "booking")
            {
                await HandleBookingCallbackAsync(callbackQuery, parts[1], entityId);
                return;
            }

            if (parts[0] == "subscription")
            {
                await HandleSubscriptionCallbackAsync(callbackQuery, parts[1], entityId);
            }
        }

        private async Task HandleBookingCallbackAsync(CallbackQuery callbackQuery, string action, Guid bookingId)
        {
            var chatId = callbackQuery.Message?.Chat.Id.ToString();
            if (string.IsNullOrWhiteSpace(chatId))
                return;

            var binding = await _context.TelegramChatBindings
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.TelegramChatId == chatId && x.IsActive && !x.IsDeleted);

            if (binding == null)
            {
                await _botClient.AnswerCallbackQuery(callbackQuery.Id, "Сначала привяжите Telegram через /start.");
                return;
            }

            var trainer = await _context.Trainers
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == binding.UserId && t.IsActive);

            if (trainer == null)
            {
                await _botClient.AnswerCallbackQuery(callbackQuery.Id, "Это действие доступно только тренеру.");
                return;
            }

            var booking = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Trainer).ThenInclude(t => t.User)
                .Include(b => b.Service)
                .Include(b => b.Subscription).ThenInclude(s => s!.SubscriptionPlan)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.TrainerId == trainer.Id);

            using var scope = _serviceProvider.CreateScope();
            var subscriptionService = scope.ServiceProvider.GetRequiredService<SubscriptionService>();

            if (booking == null)
            {
                await _botClient.AnswerCallbackQuery(callbackQuery.Id, "Запись не найдена.");
                return;
            }

            if (booking.Status == "Completed")
            {
                await _botClient.AnswerCallbackQuery(callbackQuery.Id, "Занятие уже завершено.");
                return;
            }

            if (booking.Status == "Cancelled")
            {
                await _botClient.AnswerCallbackQuery(callbackQuery.Id, "Запись уже отменена.");
                return;
            }

            if (action == "confirm")
            {
                booking.Status = "Confirmed";
                booking.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                await subscriptionService.ConsumeSubscriptionSessionAsync(booking);
                await SendBookingConfirmationAsync(booking);
                await _botClient.AnswerCallbackQuery(callbackQuery.Id, "Запись подтверждена.");
            }
            else if (action == "cancel")
            {
                booking.Status = "Cancelled";
                booking.UpdatedAt = DateTime.UtcNow;

                await subscriptionService.RestoreSubscriptionSessionAsync(booking);

                if (booking.GroupLessonSlotId == null)
                {
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
                }

                await _context.SaveChangesAsync();
                await SendBookingCancellationAsync(booking);
                await _botClient.AnswerCallbackQuery(callbackQuery.Id, "Запись отменена.");
            }

            if (callbackQuery.Message != null)
            {
                var localStartTime = ConvertToMoscowTime(booking.StartTime);
                var statusText = booking.Status switch
                {
                    "Confirmed" => "Подтверждено",
                    "Cancelled" => "Отменено",
                    _ => "Ожидает подтверждения"
                };

                var updatedText = $"""
                Заявка на занятие

                Клиент: {booking.User.Name}
                Дата: {localStartTime:dd.MM.yyyy}
                Время: {localStartTime:HH:mm}
                Услуга: {booking.Service?.Name}
                Статус: {statusText}
                """;

                await _botClient.EditMessageText(
                    chatId: callbackQuery.Message.Chat.Id,
                    messageId: callbackQuery.Message.MessageId,
                    text: updatedText);
            }
        }

        private async Task HandleSubscriptionCallbackAsync(CallbackQuery callbackQuery, string action, Guid subscriptionId)
        {
            var approverRecipient = await ResolveApproverRecipientAsync();
            var callbackChatId = callbackQuery.Message?.Chat.Id.ToString();

            if (approverRecipient == null || callbackChatId != approverRecipient.ChatId)
            {
                await _botClient.AnswerCallbackQuery(callbackQuery.Id, "Это действие доступно только подтверждающему.");
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            var subscriptionService = scope.ServiceProvider.GetRequiredService<SubscriptionService>();

            Subscription? subscription = null;
            if (action == "approve")
            {
                subscription = await subscriptionService.ApproveSubscriptionAsync(subscriptionId);
                await _botClient.AnswerCallbackQuery(callbackQuery.Id, subscription == null ? "Заявка уже обработана." : "Абонемент подтверждён.");
            }
            else if (action == "reject")
            {
                subscription = await subscriptionService.RejectSubscriptionAsync(subscriptionId, "Заявка отклонена подтверждающим.");
                await _botClient.AnswerCallbackQuery(callbackQuery.Id, subscription == null ? "Заявка уже обработана." : "Заявка отклонена.");
            }

            if (callbackQuery.Message != null && subscription != null)
            {
                var statusText = subscription.Status switch
                {
                    "Active" => "Подтверждён",
                    "Rejected" => "Отклонён",
                    "Expired" => "Истёк",
                    _ => subscription.Status
                };

                var deadline = ConvertToMoscowTime(subscription.PaymentDeadlineAt);
                var updatedText = $"""
                Заявка на абонемент

                Клиент: {subscription.User.Name}
                План: {subscription.SubscriptionPlan.Name}
                Формат: {GetFormatLabel(subscription.SubscriptionPlan.Format)}
                Занятий: {subscription.TotalSessions}
                Стоимость: {subscription.SubscriptionPlan.Price:0.##} ₽
                Дедлайн оплаты: {deadline:dd.MM.yyyy HH:mm}
                Статус: {statusText}
                """;

                await _botClient.EditMessageText(
                    chatId: callbackQuery.Message.Chat.Id,
                    messageId: callbackQuery.Message.MessageId,
                    text: updatedText);
            }
        }

        private async Task<ValikuloDance.Domain.Entities.User?> FindUserByTelegramUsernameAsync(string telegramUsername)
        {
            var normalized = NormalizeUsername(telegramUsername);
            if (string.IsNullOrWhiteSpace(normalized))
                return null;

            var withAt = "@" + normalized;

            return await _context.Users.FirstOrDefaultAsync(u =>
                EF.Functions.ILike(u.TelegramUsername, normalized) ||
                EF.Functions.ILike(u.TelegramUsername, withAt));
        }

        private async Task SendMessageAsync(TelegramRecipient recipient, string message, InlineKeyboardMarkup? replyMarkup = null)
        {
            try
            {
                var chatId = long.TryParse(recipient.ChatId, out var numericChatId)
                    ? new ChatId(numericChatId)
                    : new ChatId(recipient.ChatId);

                await _botClient.SendMessage(chatId: chatId, text: message, replyMarkup: replyMarkup);
                _logger.LogInformation("Telegram message sent to {TelegramRecipient}", recipient.LogValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отправке уведомления пользователю {TelegramRecipient}", recipient.LogValue);
            }
        }

        private async Task<TelegramRecipient?> ResolveRecipientAsync(Guid userId, string? fallbackChatId, string? fallbackUsername)
        {
            var binding = await _context.TelegramChatBindings
                .AsNoTracking()
                .Where(x => x.UserId == userId && x.IsActive && !x.IsDeleted)
                .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
                .FirstOrDefaultAsync();

            if (binding != null)
            {
                return new TelegramRecipient(
                    binding.TelegramChatId,
                    !string.IsNullOrWhiteSpace(binding.TelegramUsername) ? binding.TelegramUsername : binding.TelegramChatId);
            }

            if (!string.IsNullOrWhiteSpace(fallbackChatId))
            {
                return new TelegramRecipient(fallbackChatId, fallbackUsername ?? fallbackChatId);
            }

            return null;
        }

        private async Task<TelegramRecipient?> ResolveApproverRecipientAsync()
        {
            if (!string.IsNullOrWhiteSpace(_subscriptionSettings.ApproverTelegramChatId))
            {
                return new TelegramRecipient(
                    _subscriptionSettings.ApproverTelegramChatId.Trim(),
                    _subscriptionSettings.ApproverTelegramUsername ?? _subscriptionSettings.ApproverTelegramChatId.Trim());
            }

            var username = NormalizeUsername(_subscriptionSettings.ApproverTelegramUsername);
            if (string.IsNullOrWhiteSpace(username))
                return null;

            var binding = await _context.TelegramChatBindings
                .AsNoTracking()
                .Where(x => x.IsActive && !x.IsDeleted && x.TelegramUsername != null)
                .FirstOrDefaultAsync(x => EF.Functions.ILike(x.TelegramUsername!, username));

            if (binding == null)
            {
                return null;
            }

            return new TelegramRecipient(binding.TelegramChatId, binding.TelegramUsername ?? binding.TelegramChatId);
        }

        private string BuildPaymentDetailsMessage()
        {
            var lines = new List<string>();

            if (!string.IsNullOrWhiteSpace(_subscriptionSettings.PaymentLink))
            {
                lines.Add($"Ссылка на оплату: {_subscriptionSettings.PaymentLink}");
            }

            if (!string.IsNullOrWhiteSpace(_subscriptionSettings.PaymentCardDetails))
            {
                lines.Add($"Реквизиты карты: {_subscriptionSettings.PaymentCardDetails}");
            }

            if (lines.Count == 0)
            {
                lines.Add("Реквизиты для оплаты пока не настроены. Пожалуйста, добавьте их в backend-конфиг SubscriptionWorkflow.");
            }

            return string.Join(Environment.NewLine, lines);
        }

        private static string GetBookingPriceText(Booking booking)
        {
            if (booking.PaymentMode == "Subscription")
            {
                return "0 ₽ (по абонементу)";
            }

            var price = booking.PriceAtBooking > 0 ? booking.PriceAtBooking : booking.Service?.Price ?? 0;
            return $"{price:0.##} ₽";
        }

        private static string GetFormatLabel(string format)
        {
            return string.Equals(format, "Group", StringComparison.OrdinalIgnoreCase)
                ? "Групповой"
                : "Индивидуальный";
        }

        private static string? NormalizeUsername(string? telegramUsername)
        {
            if (string.IsNullOrWhiteSpace(telegramUsername))
                return null;

            return telegramUsername.Trim().TrimStart('@');
        }

        private static DateTime ConvertToMoscowTime(DateTime utcDateTime)
        {
            var normalizedUtc = utcDateTime.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc)
                : utcDateTime.ToUniversalTime();

            return TimeZoneInfo.ConvertTimeFromUtc(normalizedUtc, GetMoscowTimeZone());
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

        private sealed record TelegramRecipient(string ChatId, string LogValue);
    }
}
