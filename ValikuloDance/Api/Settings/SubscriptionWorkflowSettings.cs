namespace ValikuloDance.Api.Settings
{
    public class SubscriptionWorkflowSettings
    {
        public string? ApproverTelegramChatId { get; set; }
        public string? ApproverTelegramUsername { get; set; }
        public string? PaymentLink { get; set; }
        public string? PaymentCardDetails { get; set; }
        public Guid? DefaultGroupTrainerId { get; set; }
        public string[] GroupLessonStartTimes { get; set; } = ["19:00"];
        public int GroupLessonCapacity { get; set; } = 12;
        public int GroupLessonHorizonDays { get; set; } = 45;
    }
}
