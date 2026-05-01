namespace ValikuloDance.Domain.Entities
{
    public class SubscriptionPlan : BaseEntity
    {
        public required string Name { get; set; }
        public string? Description { get; set; }
        public required string Format { get; set; } = "Individual";
        public int SessionsCount { get; set; }
        public int ValidityMonths { get; set; }
        public decimal Price { get; set; }
        public Guid? SourceServiceId { get; set; }
        public bool IsActive { get; set; } = true;

        public virtual Service? SourceService { get; set; }
        public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    }
}
