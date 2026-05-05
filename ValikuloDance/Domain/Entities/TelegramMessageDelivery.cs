namespace ValikuloDance.Domain.Entities
{
    public class TelegramMessageDelivery : BaseEntity
    {
        public Guid? UserId { get; set; }
        public required string RecipientChatId { get; set; }
        public required string RecipientLogValue { get; set; }
        public required string MessageType { get; set; }
        public Guid? RelatedEntityId { get; set; }
        public required string Status { get; set; }
        public int Attempts { get; set; }
        public int? TelegramMessageId { get; set; }
        public int? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime? LastAttemptAt { get; set; }
        public DateTime? SentAt { get; set; }

        public virtual User? User { get; set; }
    }
}
