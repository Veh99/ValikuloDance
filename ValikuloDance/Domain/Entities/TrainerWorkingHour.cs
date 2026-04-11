namespace ValikuloDance.Domain.Entities
{
    public class TrainerWorkingHour : BaseEntity
    {
        public Guid TrainerId { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public TimeSpan StartTimeLocal { get; set; }
        public TimeSpan EndTimeLocal { get; set; }
        public int SlotDurationMinutes { get; set; } = 15;
        public bool IsActive { get; set; } = true;

        public virtual Trainer Trainer { get; set; } = null!;
    }
}
