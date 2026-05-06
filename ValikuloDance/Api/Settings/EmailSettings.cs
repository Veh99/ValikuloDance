namespace ValikuloDance.Api.Settings
{
    public class EmailSettings
    {
        public string? SmtpHost { get; set; }
        public int SmtpPort { get; set; } = 587;
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string FromEmail { get; set; } = "no-reply@valikulodance.com";
        public string FromName { get; set; } = "Valikulo Dance";
        public bool EnableSsl { get; set; } = true;
    }
}
