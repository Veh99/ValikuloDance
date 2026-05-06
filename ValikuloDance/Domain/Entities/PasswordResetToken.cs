namespace ValikuloDance.Domain.Entities
{
    public class PasswordResetToken : BaseEntity
    {
        public required Guid UserId { get; set; }
        public required string TokenHash { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? UsedAt { get; set; }
        public int AttemptsCount { get; set; }

        public virtual User User { get; set; } = null!;
    }
}
