namespace ValikuloDance.Domain.Entities
{
    public class GroupLessonSlot : BaseEntity
    {
        public Guid ServiceId { get; set; }
        public Guid TrainerId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int Capacity { get; set; }
        public bool IsActive { get; set; } = true;

        public virtual Service Service { get; set; } = null!;
        public virtual Trainer Trainer { get; set; } = null!;
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}
