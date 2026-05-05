using Telegram.Bot.Types;
using ValikuloDance.Application.DTOs.Telegram;
using ValikuloDance.Domain.Entities;

namespace ValikuloDance.Application.Interfaces
{
    public interface ITelegramService
    {
        Task<TelegramNotificationResult> SendBookingPendingAsync(Booking booking);
        Task<TelegramNotificationResult> SendBookingConfirmationAsync(Booking booking);
        Task<TelegramNotificationResult> SendBookingReminderAsync(Booking booking);
        Task<TelegramNotificationResult> SendBookingCancellationAsync(Booking booking);
        Task<TelegramNotificationResult> SendSubscriptionRequestCreatedAsync(Subscription subscription);
        Task<TelegramNotificationResult> SendSubscriptionApprovedAsync(Subscription subscription);
        Task<TelegramNotificationResult> SendSubscriptionRejectedAsync(Subscription subscription);
        Task<TelegramNotificationResult> SendSubscriptionExpiredAsync(Subscription subscription);
        Task UpsertChatBindingAsync(Guid userId, string telegramChatId, string? telegramUsername);
        Task HandleUpdateAsync(Update update);
        Task<bool> HasActiveChatBindingAsync(Guid userId);
    }
}
