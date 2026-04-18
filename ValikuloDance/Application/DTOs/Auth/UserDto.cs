namespace ValikuloDance.Application.DTOs.Auth
{
    public class UserDto
    {
        public required Guid Id { get; set; }
        public required string Name { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public required string Role { get; set; }
        public required string TelegramUsername { get; set; }
        public bool IsTrainer { get; set; }
        public Guid? TrainerId { get; set; }
        public required DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }
}
