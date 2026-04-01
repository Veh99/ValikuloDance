namespace ValikuloDance.Application.DTOs.Trainer
{
    public class TrainerDto
    {
        public required Guid UserId { get; set; }
        public required string Bio {  get; set; }
        public required int ExperienceYears { get; set; }
        public required List<string> DanceStyles { get; set; } = new();
        public string? PhotoUrl { get; set; }
    }
}
