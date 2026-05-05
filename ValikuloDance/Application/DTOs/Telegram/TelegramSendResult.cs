namespace ValikuloDance.Application.DTOs.Telegram
{
    public class TelegramSendResult
    {
        public bool Success { get; init; }
        public bool RecipientConfigured { get; init; }
        public string? MessageType { get; init; }
        public Guid? DeliveryId { get; init; }
        public int? TelegramMessageId { get; init; }
        public int? ErrorCode { get; init; }
        public string? ErrorMessage { get; init; }

        public static TelegramSendResult MissingRecipient(string? errorMessage = null)
        {
            return new TelegramSendResult
            {
                Success = false,
                RecipientConfigured = false,
                MessageType = null,
                ErrorMessage = errorMessage ?? "Telegram chat is not bound"
            };
        }
    }

    public class TelegramNotificationResult
    {
        public List<TelegramSendResult> Sends { get; } = new();
        public bool AllRequiredSent => Sends.Count > 0 && Sends.All(x => x.Success);
        public string? FirstError => Sends.FirstOrDefault(x => !x.Success)?.ErrorMessage;

        public bool IsMessageTypeSent(string messageType)
        {
            return Sends.Any(x => x.Success && string.Equals(x.MessageType, messageType, StringComparison.OrdinalIgnoreCase));
        }
    }
}
