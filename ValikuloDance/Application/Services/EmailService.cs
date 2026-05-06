using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using ValikuloDance.Api.Settings;
using ValikuloDance.Application.Interfaces;

namespace ValikuloDance.Application.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string resetUrl)
        {
            if (string.IsNullOrWhiteSpace(_settings.SmtpHost))
            {
                _logger.LogWarning("Password reset email was not sent because Email:SmtpHost is not configured.");
                return;
            }

            using var message = new MailMessage
            {
                From = new MailAddress(_settings.FromEmail, _settings.FromName),
                Subject = "Восстановление пароля Valikulo Dance",
                Body = $"""
                Здравствуйте!

                Для восстановления пароля перейдите по ссылке:
                {resetUrl}

                Ссылка действует 30 минут. Если вы не запрашивали восстановление пароля, просто проигнорируйте это письмо.
                """,
                IsBodyHtml = false
            };
            message.To.Add(toEmail);

            using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
            {
                EnableSsl = _settings.EnableSsl
            };

            if (!string.IsNullOrWhiteSpace(_settings.Username))
            {
                client.Credentials = new NetworkCredential(_settings.Username, _settings.Password);
            }

            await client.SendMailAsync(message);
        }
    }
}
