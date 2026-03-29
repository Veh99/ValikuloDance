namespace ValikuloDance.Domain.Entities
{
    public class Subscription : BaseEntity
    {
        public Guid UserId { get; set; }
        public Guid ServiceId { get; set; }
        public int TotalSessions { get; set; }
        public int UsedSessions { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsActive { get; set; } = true;

        // Навигационные свойства
        public virtual User User { get; set; }
        public virtual Service Service { get; set; }
    }
}
