using System.Text;
using System.Text.Json;
using ValikuloDance.Application.Interfaces;
using ValikuloDance.Domain.Entities;

namespace ValikuloDance.Application.Services
{
    public class TelegramService : ITelegramService
    {
        private readonly HttpClient _httpClient;
        private readonly string _botToken;
        private readonly ILogger<TelegramService> _logger;

        public TelegramService(IConfiguration configuration, ILogger<TelegramService> logger)
        {
            _httpClient = new HttpClient();
            _botToken = configuration["Telegram:BotToken"];
            _logger = logger;
        }

        public async Task SendBookingConfirmationAsync(Booking booking)
        {
            var user = booking.User;
            var trainer = booking.Trainer.User;
            var service = booking.Service;

            var message = $@"
                            ✨ <b>Новая запись!</b> ✨
                            Вы записаны на занятие:
                            📅 {booking.StartTime:dd.MM.yyyy}
                            ⏰ {booking.StartTime:HH:mm}
                            💃 {service.Name}
                            👨‍🏫 Тренер: {trainer.Name}
                            💰 Оплата: {service.Price} ₽ (на месте)
                            Ждем вас в студии! 💪";

            await SendMessageAsync(user.TelegramChatId, message);

            // Отправляем уведомление тренеру
            var trainerMessage = $@"
                                🕺 <b>Новая запись на занятие!</b
                                👤 Клиент: {user.Name}
                                📅 {booking.StartTime:dd.MM.yyyy}
                                ⏰ {booking.StartTime:HH:mm}
                                💃 {service.Name}
                                Подготовьтесь к занятию!";

            await SendMessageAsync(trainer.TelegramChatId, trainerMessage);
        }

        public async Task SendBookingReminderAsync(Booking booking)
        {
            var message = $@"⏰ <b>Напоминание о занятии!</b>
                            Сегодня в {booking.StartTime:HH:mm} у вас занятие {booking.Service.Name} с тренером {booking.Trainer.User.Name}.
                            Ждем вас в студии! 🕺";

            await SendMessageAsync(booking.User.TelegramChatId, message);
        }

        public async Task SendBookingCancellationAsync(Booking booking)
        {
            var message = $@"❌ <b>Запись отменена</b>
                            Занятие {booking.Service.Name} на {booking.StartTime:dd.MM.yyyy HH:mm} отменено.
                            Если у вас есть вопросы, свяжитесь с администратором.";

            await SendMessageAsync(booking.User.TelegramChatId, message);

            // Уведомляем тренера
            var trainerMessage = $@"❌ <b>Запись отменена клиентом</b>
                                 Клиент {booking.User.Name} отменил занятие {booking.Service.Name} на {booking.StartTime:dd.MM.yyyy HH:mm}.";

            await SendMessageAsync(booking.Trainer.User.TelegramChatId, trainerMessage);
        }

        public async Task SendWelcomeMessageAsync(string chatId, string name)
        {
            var message = $@"
                🎉 <b>Добро пожаловать в Dance Studio!</b>

                Привет, {name}!

                Теперь вы будете получать уведомления о записях и напоминания о занятиях.

                ✨ Чтобы записаться на занятие, перейдите на наш сайт: https://dancestudio.ru/booking

                Если у вас есть вопросы, напишите администратору.";

            await SendMessageAsync(chatId, message);
        }

        public async Task<bool> VerifyUserAsync(string chatId)
        {
            try
            {
                var url = $"https://api.telegram.org/bot{_botToken}/getChat";
                var payload = new { chat_id = chatId };
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при проверке Telegram пользователя");
                return false;
            }
        }

        private async Task SendMessageAsync(string chatId, string message)
        {
            if (string.IsNullOrEmpty(chatId) || string.IsNullOrEmpty(_botToken))
                return;

            try
            {
                var url = $"https://api.telegram.org/bot{_botToken}/sendMessage";
                var payload = new
                {
                    chat_id = chatId,
                    text = message,
                    parse_mode = "HTML"
                };

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Ошибка отправки сообщения в Telegram: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отправке сообщения в Telegram");
            }
        }
    }
}