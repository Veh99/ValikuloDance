using Telegram.Bot.Types;
using ValikuloDance.Domain.Entities;

namespace ValikuloDance.Application.Interfaces
{
    public interface ITelegramService
    {
        Task SendBookingPendingAsync(Booking booking);
        Task SendBookingConfirmationAsync(Booking booking);
        Task SendBookingReminderAsync(Booking booking);
        Task SendBookingCancellationAsync(Booking booking);
        Task UpsertChatBindingAsync(Guid userId, string telegramChatId, string? telegramUsername);
        Task HandleUpdateAsync(Update update);
    }
}
