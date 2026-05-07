using System.Net;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ValikuloDance.Api.Settings;
using ValikuloDance.Application.Interfaces;

namespace ValikuloDance.Application.Services
{
    public class EmailService : IEmailService
    {
        private const string PasswordResetSubject = "Восстановление пароля Valikulo Dance";

        private readonly EmailSettings _settings;
        private readonly ILogger<EmailService> _logger;
        private readonly HttpClient _httpClient;

        public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger, HttpClient httpClient)
        {
            _settings = settings.Value;
            _logger = logger;
            _httpClient = httpClient;
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string resetUrl)
        {
            try
            {
                await SendPasswordResetEmailWithProviderAsync(_settings.Provider, toEmail, resetUrl);
            }
            catch (Exception ex) when (!string.IsNullOrWhiteSpace(_settings.FallbackProvider))
            {
                _logger.LogError(
                    ex,
                    "Password reset email failed with provider {Provider}. Trying fallback provider {FallbackProvider}.",
                    _settings.Provider,
                    _settings.FallbackProvider);

                await SendPasswordResetEmailWithProviderAsync(_settings.FallbackProvider, toEmail, resetUrl);
            }
        }

        private async Task SendPasswordResetEmailWithProviderAsync(string? provider, string toEmail, string resetUrl)
        {
            if (string.Equals(provider, "CustomerIo", StringComparison.OrdinalIgnoreCase))
            {
                await SendPasswordResetEmailWithCustomerIoAsync(toEmail, resetUrl);
                return;
            }

            if (string.Equals(provider, "Smtp", StringComparison.OrdinalIgnoreCase))
            {
                await SendPasswordResetEmailWithSmtpAsync(toEmail, resetUrl);
                return;
            }

            throw new InvalidOperationException($"Unsupported email provider: {provider}");
        }

        private async Task SendPasswordResetEmailWithCustomerIoAsync(string toEmail, string resetUrl)
        {
            if (string.IsNullOrWhiteSpace(_settings.CustomerIoApiKey))
            {
                _logger.LogWarning("Password reset email was not sent because Email:CustomerIoApiKey is not configured.");
                return;
            }

            var textContent = BuildPasswordResetText(resetUrl);
            var payload = new
            {
                to = toEmail,
                from = $"{_settings.FromName} <{_settings.FromEmail}>",
                subject = PasswordResetSubject,
                body = textContent,
                body_plain = textContent
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, _settings.CustomerIoApiUrl)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };

            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.CustomerIoApiKey);

            using var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogError(
                    "Customer.io password reset email failed with status {StatusCode}: {ResponseBody}",
                    (int)response.StatusCode,
                    responseBody);
            }

            response.EnsureSuccessStatusCode();
        }

        private async Task SendPasswordResetEmailWithSmtpAsync(string toEmail, string resetUrl)
        {
            if (string.IsNullOrWhiteSpace(_settings.SmtpHost))
            {
                _logger.LogWarning("Password reset email was not sent because Email:SmtpHost is not configured.");
                return;
            }

            using var message = new MailMessage
            {
                From = new MailAddress(_settings.FromEmail, _settings.FromName),
                Subject = PasswordResetSubject,
                Body = BuildPasswordResetText(resetUrl),
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

        private static string BuildPasswordResetText(string resetUrl)
        {
            return $"""
            Здравствуйте!

            Для восстановления пароля перейдите по ссылке:
            {resetUrl}

            Ссылка действует 30 минут. Если вы не запрашивали восстановление пароля, просто проигнорируйте это письмо.
            """;
        }
    }
}
