namespace ValikuloDance.Domain.Entities
{
    public class Booking : BaseEntity
    {
        public Guid UserId { get; set; }
        public Guid TrainerId { get; set; }
        public Guid ServiceId { get; set; }
        public Guid? SubscriptionId { get; set; }
        public Guid? GroupLessonSlotId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Status { get; set; } = "Pending";
        public string PaymentMode { get; set; } = "Single";
        public bool PaidOnSite { get; set; }
        public decimal PriceAtBooking { get; set; }
        public bool IsSubscriptionSessionConsumed { get; set; }
        public string? Notes { get; set; }

        public virtual User User { get; set; } = null!;
        public virtual Trainer Trainer { get; set; } = null!;
        public virtual Service Service { get; set; } = null!;
        public virtual Subscription? Subscription { get; set; }
        public virtual GroupLessonSlot? GroupLessonSlot { get; set; }
        public virtual ICollection<ScheduleSlot> ScheduleSlots { get; set; } = new List<ScheduleSlot>();
    }
}
