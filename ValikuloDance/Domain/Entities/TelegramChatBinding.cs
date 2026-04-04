namespace ValikuloDance.Domain.Entities
{
    public class TelegramChatBinding : BaseEntity
    {
        public required Guid UserId { get; set; }
        public required string TelegramChatId { get; set; }
        public string? TelegramUsername { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? LastVerifiedAt { get; set; }

        public virtual User User { get; set; } = null!;
    }
}
