namespace ValikuloDance.Domain.Entities
{
    public class TrainerScheduleOverride : BaseEntity
    {
        public Guid TrainerId { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan? StartTimeLocal { get; set; }
        public TimeSpan? EndTimeLocal { get; set; }
        public string Type { get; set; } = "Unavailable";
        public int? SlotDurationMinutes { get; set; }
        public string? Reason { get; set; }
        public bool IsActive { get; set; } = true;

        public virtual Trainer Trainer { get; set; } = null!;
    }
}
