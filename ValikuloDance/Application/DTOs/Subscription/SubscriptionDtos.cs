namespace ValikuloDance.Application.DTOs.Subscription
{
    public class SubscriptionPlanResponse
    {
        public required Guid Id { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public required string Format { get; set; }
        public Guid? SourceServiceId { get; set; }
        public required int SessionsCount { get; set; }
        public required int ValidityMonths { get; set; }
        public required decimal Price { get; set; }
    }

    public class CreateSubscriptionRequestDto
    {
        public Guid SubscriptionPlanId { get; set; }
        public string? Comment { get; set; }
    }

    public class SubscriptionResponse
    {
        public required Guid Id { get; set; }
        public required Guid SubscriptionPlanId { get; set; }
        public required string PlanName { get; set; }
        public required string Format { get; set; }
        public required int TotalSessions { get; set; }
        public required int UsedSessions { get; set; }
        public required int RemainingSessions { get; set; }
        public required int ValidityMonths { get; set; }
        public required decimal Price { get; set; }
        public required string Status { get; set; }
        public required DateTime RequestedAt { get; set; }
        public DateTime? PaymentDeadlineAt { get; set; }
        public DateTime? StartsAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string? RejectionReason { get; set; }
        public bool CanBeUsedForBooking { get; set; }
    }

    public class ActiveSubscriptionOptionResponse
    {
        public required Guid Id { get; set; }
        public required string PlanName { get; set; }
        public required string Format { get; set; }
        public Guid? SourceServiceId { get; set; }
        public required int RemainingSessions { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    public class GroupLessonSlotResponse
    {
        public required Guid Id { get; set; }
        public required Guid ServiceId { get; set; }
        public required Guid TrainerId { get; set; }
        public Guid? GroupLessonScheduleId { get; set; }
        public required string ServiceName { get; set; }
        public required string TrainerName { get; set; }
        public required DateTime StartTime { get; set; }
        public required DateTime EndTime { get; set; }
        public required int Capacity { get; set; }
        public required int AvailableSpots { get; set; }
    }

    public class CreateGroupBookingRequest
    {
        public Guid GroupLessonSlotId { get; set; }
        public string PaymentMode { get; set; } = "Single";
        public Guid? SubscriptionId { get; set; }
        public string? Notes { get; set; }
    }

    public class GroupLessonScheduleResponse
    {
        public required Guid Id { get; set; }
        public required Guid TrainerId { get; set; }
        public required Guid ServiceId { get; set; }
        public required string ServiceName { get; set; }
        public required string TrainerName { get; set; }
        public required DayOfWeek DayOfWeek { get; set; }
        public required string StartTimeLocal { get; set; }
        public required int Capacity { get; set; }
        public required bool IsActive { get; set; }
    }

    public class UpsertGroupLessonScheduleRequest
    {
        public Guid ServiceId { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public required string StartTimeLocal { get; set; }
        public int? Capacity { get; set; }
    }
}
