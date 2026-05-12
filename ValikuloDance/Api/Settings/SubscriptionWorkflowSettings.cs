namespace ValikuloDance.Api.Settings
{
    public class SubscriptionWorkflowSettings
    {
        public string? ApproverTelegramChatId { get; set; }
        public string? ApproverTelegramUsername { get; set; }
        public string? PaymentLink { get; set; }
        public string? PaymentCardDetails { get; set; }
        public int GroupLessonCapacity { get; set; } = 12;
        public int GroupLessonHorizonDays { get; set; } = 45;
    }
}
