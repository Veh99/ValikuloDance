using Telegram.Bot;
using Telegram.Bot.Types;
using ValikuloDance.Application.Interfaces;
using ValikuloDance.Domain.Entities;

namespace ValikuloDance.Application.Services
{
    public class TelegramService : ITelegramService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly ILogger<TelegramService> _logger;

        public TelegramService(IConfiguration configuration, ILogger<TelegramService> logger)
        {
            _logger = logger;
            var botToken = configuration["Telegram:BotToken"]!;
            _botClient = new TelegramBotClient(botToken);
        }

        public async Task SendBookingConfirmationAsync(Booking booking)
        {
            try
            {
                if (booking == null)
                {
                    _logger.LogWarning("Booking is null");
                    return;
                }

                var user = booking.User;
                var trainer = booking.Trainer?.User;
                var service = booking.Service;

                if (user == null)
                {
                    _logger.LogWarning($"User not found for booking {booking.Id}");
                    return;
                }

                var dateStr = booking.StartTime.ToString("dd.MM.yyyy");
                var timeStr = booking.StartTime.ToString("HH:mm");
                var serviceName = service?.Name ?? "Услуга";
                var servicePrice = service?.Price.ToString() ?? "0";
                var trainerName = trainer?.Name ?? "Тренер";

                if (!string.IsNullOrEmpty(user.TelegramUsername))
                {
                    var userMessage = $"""
                    ✨ НОВАЯ ЗАПИСЬ! ✨
                    
                    Вы записаны на занятие:
                    📅 {dateStr}
                    ⏰ {timeStr}
                    💃 {serviceName}
                    👨‍🏫 Тренер: {trainerName}
                    💰 Стоимость: {servicePrice} ₽
                    
                    Ждем вас на паркете! 💪
                    """;

                    await SendMessageAsync(user.TelegramUsername, userMessage);
                }
                else
                {
                    _logger.LogWarning($"Ошибка при отправке сообщения пользователю {user.Id}, проверьте юзернейм");
                }

                if (trainer != null && !string.IsNullOrEmpty(trainer.TelegramUsername))
                {
                    var trainerMessage = $"""
                    🕺 НОВАЯ ЗАПИСЬ НА ЗАНЯТИЕ!
                    
                    👤 Клиент: {user.Name ?? "Клиент"}
                    📅 {dateStr}
                    ⏰ {timeStr}
                    💃 {serviceName}
                    
                    Подготовьтесь к занятию!
                    """;

                    await SendMessageAsync(trainer.TelegramUsername, trainerMessage);
                }
                else
                {
                    _logger.LogWarning($"Ошибка при отправке сообщения тренеру, проверьте юзернейм {booking.Id}");
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
                {
                    _logger.LogWarning("Запись или пользователь отсутствуют в базе данных");
                    return;
                }

                var message = $"""
                ⏰ НАПОМИНАНИЕ О ЗАНЯТИИ!
                
                Сегодня в {booking.StartTime:HH:mm} у вас занятие {booking.Service?.Name} с тренером {booking.Trainer?.User?.Name}.
                Ждем вас в студии! 🕺
                """;

                await SendMessageAsync(booking.User.TelegramUsername, message);
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
                {
                    _logger.LogWarning("Пришла несуществующая запись");
                    return;
                }

                // Уведомляем пользователя
                var userMessage = $"""
                    ❌ ЗАПИСЬ ОТМЕНЕНА
                
                    Занятие {booking.Service?.Name} на {booking.StartTime:dd.MM.yyyy HH:mm} отменено.
                    Если у вас есть вопросы, свяжитесь с администратором.
                    """;

                await SendMessageAsync(booking.User.TelegramUsername, userMessage);

                // Уведомляем тренера
                if (booking.Trainer?.User != null)
                {
                    var trainerMessage = $"""
                        ❌ ЗАПИСЬ ОТМЕНЕНА КЛИЕНТОМ
                    
                        Клиент {booking.User.Name} отменил занятие {booking.Service?.Name} на {booking.StartTime:dd.MM.yyyy HH:mm}.
                        """;

                    await SendMessageAsync(booking.Trainer.User.TelegramUsername, trainerMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending booking cancellation");
            }
        }

        private async Task SendMessageAsync(string username, string message)
        {
            if (string.IsNullOrEmpty(username))
            {
                _logger.LogError($"Невозможно отправить сообщение, потому что заполнен пустой юзернейм");
                return;
            }

            try
            {
                var cleanUsername = username.StartsWith("@") ? username.Substring(1) : username;

                var updates = await _botClient.GetUpdates();

                var update = updates.FirstOrDefault(u =>
                    u.Message?.Chat?.Username?.Equals(cleanUsername, StringComparison.OrdinalIgnoreCase) == true);

                if (update?.Message?.Chat == null)
                {
                    _logger.LogError($"У пользователя нет чата с ботом @{username}. Чтобы получать уведомления, надо начать диалог с ботом");
                    return;
                }
                var chat = update.Message.Chat;
                var chatId = chat.Id;

                await _botClient.SendMessage(
                    chatId: new ChatId(chatId),
                    text: message
                );

                _logger.LogInformation($"Успешно отправлено телеграм-уведомление пользователю {username}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при отправке уведомления пользователю {username}");
            }
        }
    }
}