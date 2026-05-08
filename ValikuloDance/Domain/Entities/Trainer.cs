namespace ValikuloDance.Domain.Entities
{
    public class Trainer : BaseEntity
    {
        public required string Bio { get; set; }
        public required string PhotoUrl { get; set; }
        public required int ExperienceYears { get; set; }
        public required List<string> DanceStyles { get; set; } = new();
        public string? Instagram { get; set; }
        public string? Telegram { get; set; }
        public bool IsActive { get; set; } = true;
        public required Guid UserId { get; set; }

        // Навигационные свойства
        public virtual User? User { get; set; }
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public virtual ICollection<ScheduleSlot> ScheduleSlots { get; set; } = new List<ScheduleSlot>();
        public virtual ICollection<TrainerWorkingHour> WorkingHours { get; set; } = new List<TrainerWorkingHour>();
        public virtual ICollection<TrainerScheduleOverride> ScheduleOverrides { get; set; } = new List<TrainerScheduleOverride>();
        public virtual ICollection<GroupLessonSchedule> GroupLessonSchedules { get; set; } = new List<GroupLessonSchedule>();
    }
}
