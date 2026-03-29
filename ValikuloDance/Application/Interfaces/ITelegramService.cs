using ValikuloDance.Domain.Entities;

namespace ValikuloDance.Application.Interfaces
{
    public interface ITelegramService
    {
        Task SendBookingConfirmationAsync(Booking booking);
        Task SendBookingReminderAsync(Booking booking);
        Task SendBookingCancellationAsync(Booking booking);
        Task SendWelcomeMessageAsync(string chatId, string name);
        Task<bool> VerifyUserAsync(string chatId);
    }
}
