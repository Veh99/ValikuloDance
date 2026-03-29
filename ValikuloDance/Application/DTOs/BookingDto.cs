namespace ValikuloDance.Application.DTOs
{
    public class CreateBookingRequest
    {
        public required Guid Id { get; set; }
        public required Guid UserId { get; set; }
        public required Guid TrainerId { get; set; }
        public required Guid ServiceId { get; set; }
        public required DateTime StartTime { get; set; }
        public string? Notes { get; set; }
    }

    public class BookingResponse
    {
        public required Guid Id { get; set; }
        public required string UserName { get; set; }
        public required string TrainerName { get; set; }
        public required string ServiceName { get; set; }
        public required DateTime StartTime { get; set; }
        public required DateTime EndTime { get; set; }
        public required string Status { get; set; }
        public required decimal Price { get; set; }
    }

    public class AvailableSlotResponse
    {
        public required DateTime StartTime { get; set; }
        public required DateTime EndTime { get; set; }
        public required bool IsAvailable { get; set; }
    }
}
