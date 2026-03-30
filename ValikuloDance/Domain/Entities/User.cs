namespace ValikuloDance.Domain.Entities
{
    public class User : BaseEntity
    {

        public required string Email { get; set; }
        public required string Phone { get; set; }
        public required string Name { get; set; }
        public required string TelegramChatId { get; set; }
        public required string TelegramUsername { get; set; }
        public required string Role { get; set; } = "Client";
        public DateTime? LastLoginAt { get; set; }

        // Поля для аутентификации
        public string PasswordHash { get; set; } = string.Empty;
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }

        // Навигационные свойства
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();

    }
}
