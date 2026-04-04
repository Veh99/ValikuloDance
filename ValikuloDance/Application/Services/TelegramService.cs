using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
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

        public TelegramService(IConfiguration configuration, ILogger<TelegramService> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
            var botToken = configuration["Telegram:BotToken"]!;
            _botClient = new TelegramBotClient(botToken);
        }

        public async Task HandleUpdateAsync(Update update)
        {
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
                    text: "Telegram успешно привязан. Теперь вы будете получать уведомления о записях.");
            }
        }

        public async Task SendBookingConfirmationAsync(Booking booking)
        {
            try
            {
                if (booking?.User == null)
                {
                    _logger.LogWarning("Booking or user is null for confirmation");
                    return;
                }

                var trainer = booking.Trainer?.User;
                var service = booking.Service;
                var dateStr = booking.StartTime.ToString("dd.MM.yyyy");
                var timeStr = booking.StartTime.ToString("HH:mm");
                var serviceName = service?.Name ?? "Услуга";
                var servicePrice = service?.Price.ToString() ?? "0";
                var trainerName = trainer?.Name ?? "Тренер";

                var userRecipient = await ResolveRecipientAsync(
                    booking.User.Id,
                    booking.User.TelegramChatId,
                    booking.User.TelegramUsername);

                if (userRecipient != null)
                {
                    var userMessage = $"""
                    Новая запись

                    Вы записаны на занятие:
                    Дата: {dateStr}
                    Время: {timeStr}
                    Услуга: {serviceName}
                    Тренер: {trainerName}
                    Стоимость: {servicePrice} ₽
                    """;

                    await SendMessageAsync(userRecipient, userMessage);
                }

                if (trainer != null)
                {
                    var trainerRecipient = await ResolveRecipientAsync(
                        trainer.Id,
                        trainer.TelegramChatId,
                        trainer.TelegramUsername);

                    if (trainerRecipient != null)
                    {
                        var trainerMessage = $"""
                        Новая запись на занятие

                        Клиент: {booking.User.Name}
                        Дата: {dateStr}
                        Время: {timeStr}
                        Услуга: {serviceName}
                        """;

                        await SendMessageAsync(trainerRecipient, trainerMessage);
                    }
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

                var message = $"""
                Напоминание о занятии

                Сегодня в {booking.StartTime:HH:mm} у вас занятие {booking.Service?.Name} с тренером {booking.Trainer?.User?.Name}.
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

                if (userRecipient != null)
                {
                    var userMessage = $"""
                    Запись отменена

                    Занятие {booking.Service?.Name} на {booking.StartTime:dd.MM.yyyy HH:mm} отменено.
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
                        Запись отменена клиентом

                        Клиент {booking.User.Name} отменил занятие {booking.Service?.Name} на {booking.StartTime:dd.MM.yyyy HH:mm}.
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

        private async Task SendMessageAsync(TelegramRecipient recipient, string message)
        {
            try
            {
                var chatId = long.TryParse(recipient.ChatId, out var numericChatId)
                    ? new ChatId(numericChatId)
                    : new ChatId(recipient.ChatId);

                await _botClient.SendMessage(chatId: chatId, text: message);

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

        private static string? NormalizeUsername(string? telegramUsername)
        {
            if (string.IsNullOrWhiteSpace(telegramUsername))
                return null;

            return telegramUsername.Trim().TrimStart('@');
        }

        private sealed record TelegramRecipient(string ChatId, string LogValue);
    }
}
