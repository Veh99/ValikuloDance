namespace ValikuloDance.Domain.Entities
{
    public class User : BaseEntity
    {
        public string? Email { get; set; }
        public required string Phone { get; set; }
        public required string Name { get; set; }
        public string? TelegramChatId { get; set; }
        public required string? TelegramUsername { get; set; }
        public required string Role { get; set; } = "Client"; // Client, Trainer, Admin
        public DateTime? LastLoginAt { get; set; }

        // Навигационные свойства
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    }
}
