namespace ValikuloDance.Domain.Entities
{
    public class Subscription : BaseEntity
    {
        public Guid UserId { get; set; }
        public Guid SubscriptionPlanId { get; set; }
        public int TotalSessions { get; set; }
        public int UsedSessions { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? PaymentDeadlineAt { get; set; }
        public DateTime? StartsAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime? RejectedAt { get; set; }
        public string Status { get; set; } = "PendingPayment";
        public string? RejectionReason { get; set; }
        public bool IsActive { get; set; } = true;

        public virtual User User { get; set; } = null!;
        public virtual SubscriptionPlan SubscriptionPlan { get; set; } = null!;
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}
