namespace ValikuloDance.Domain.Entities
{
    public class Booking : BaseEntity
    {
        public Guid UserId { get; set; }
        public Guid TrainerId { get; set; }
        public Guid ServiceId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Confirmed, Completed, Cancelled
        public bool PaidOnSite { get; set; }
        public string? Notes { get; set; }

        // Навигационные свойства
        public virtual User User { get; set; }
        public virtual Trainer Trainer { get; set; }
        public virtual Service Service { get; set; }
        public virtual ICollection<ScheduleSlot> ScheduleSlots { get; set; } = new List<ScheduleSlot>();
    }
}
