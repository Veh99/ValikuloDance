namespace ValikuloDance.Application.DTOs.Auth
{
    public class UserDto
    {
        public required Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Phone { get; set; }
        public required string? Email { get; set; }
        public required string Role { get; set; }
        public required string TelegramUsername { get; set; }
        public required DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }
}
