namespace ValikuloDance.Domain.Entities
{
    public class ApplicationUser : User
    {
        public required string PasswordHash { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
    }
}
