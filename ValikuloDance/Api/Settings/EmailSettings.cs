namespace ValikuloDance.Api.Settings
{
    public class EmailSettings
    {
        public string? SmtpHost { get; set; }
        public int SmtpPort { get; set; } = 587;
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string Provider { get; set; } = "Smtp";
        public string? FallbackProvider { get; set; }
        public string? CustomerIoApiKey { get; set; }
        public string CustomerIoApiUrl { get; set; } = "https://api.customer.io/v1/send/email";
        public string FromEmail { get; set; } = "no-reply@valikulodance.com";
        public string FromName { get; set; } = "Valikulo Dance";
        public bool EnableSsl { get; set; } = true;
    }
}
