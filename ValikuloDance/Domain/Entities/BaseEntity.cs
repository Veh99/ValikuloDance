namespace ValikuloDance.Domain.Entities
{
    public abstract class BaseEntity
    {
        public required Guid Id { get; set; }
        public required DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
    }
}
