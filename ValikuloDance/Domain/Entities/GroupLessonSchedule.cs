namespace ValikuloDance.Domain.Entities
{
    public class GroupLessonSchedule : BaseEntity
    {
        public Guid TrainerId { get; set; }
        public Guid ServiceId { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public TimeSpan StartTimeLocal { get; set; }
        public int Capacity { get; set; }
        public bool IsActive { get; set; } = true;

        public virtual Trainer Trainer { get; set; } = null!;
        public virtual Service Service { get; set; } = null!;
        public virtual ICollection<GroupLessonSlot> GroupLessonSlots { get; set; } = new List<GroupLessonSlot>();
    }
}
