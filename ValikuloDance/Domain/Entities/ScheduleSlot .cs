namespace ValikuloDance.Domain.Entities
{
    public class ScheduleSlot : BaseEntity
    {
        public Guid TrainerId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsBooked { get; set; }

        // Навигационные свойства
        public virtual Trainer Trainer { get; set; }
        public virtual Booking? Booking { get; set; }
    }
}
