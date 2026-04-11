namespace ValikuloDance.Application.DTOs.Trainer
{
    public class TrainerDto
    {
        public required Guid UserId { get; set; }
        public required string Bio {  get; set; }
        public required int ExperienceYears { get; set; }
        public required List<string> DanceStyles { get; set; } = new();
        public string? PhotoUrl { get; set; }
        public List<TrainerWorkingHourDto> WorkingHours { get; set; } = new();
    }

    public class TrainerWorkingHourDto
    {
        public required DayOfWeek DayOfWeek { get; set; }
        public required string StartTimeLocal { get; set; }
        public required string EndTimeLocal { get; set; }
        public int SlotDurationMinutes { get; set; } = 15;
    }
}
