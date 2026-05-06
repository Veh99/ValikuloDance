namespace ValikuloDance.Domain.Entities
{
    public class User : BaseEntity
    {

        public string? Email { get; set; }
        public string? Phone { get; set; }
        public required string Name { get; set; }
        public string? TelegramChatId { get; set; }
        public required string TelegramUsername { get; set; }
        public required string Role { get; set; } = "Client";
        public DateTime? LastLoginAt { get; set; }

        // Поля для аутентификации
        public string PasswordHash { get; set; } = string.Empty;
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
        public bool HasLateCancellationPenalty { get; set; }

        // Навигационные свойства
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
        public virtual ICollection<TelegramChatBinding> TelegramChatBindings { get; set; } = new List<TelegramChatBinding>();
        public virtual ICollection<TelegramMessageDelivery> TelegramMessageDeliveries { get; set; } = new List<TelegramMessageDelivery>();
        public virtual ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();
    }
}
