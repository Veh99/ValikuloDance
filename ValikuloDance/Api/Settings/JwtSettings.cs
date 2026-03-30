namespace ValikuloDance.Api.Settings
{
    public class JwtSettings
    {
        public required string SecretKey { get; set; }
        public required string Issuer { get; set; }
        public required string Audience { get; set; }
        public required int ExpirationMinutes { get; set; }
        public required int RefreshTokenExpirationDays { get; set; }
    }
}
