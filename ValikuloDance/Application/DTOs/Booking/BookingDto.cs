namespace ValikuloDance.Application.DTOs.Booking
{
    public class CreateBookingRequest
    {
        public Guid TrainerId { get; set; }
        public Guid ServiceId { get; set; }
        public DateTime StartTime { get; set; }
        public string? Notes { get; set; }
    }

    public class AvailableDateResponse
    {
        public required DateTime Date { get; set; }
        public required int AvailableSlotCount { get; set; }
    }

    public class BookingResponse
    {
        public required Guid Id { get; set; }
        public required Guid UserId { get; set; }
        public required Guid TrainerId { get; set; }
        public required Guid ServiceId { get; set; }
        public required string UserName { get; set; }
        public required string TrainerName { get; set; }
        public required string ServiceName { get; set; }
        public required DateTime StartTime { get; set; }
        public required DateTime EndTime { get; set; }
        public required string Status { get; set; }
        public required decimal Price { get; set; }
        public required bool HasPenaltyPrice { get; set; }
        public required bool CanBeCancelledByUser { get; set; }
        public string? Notes { get; set; }
    }

    public class AvailableSlotResponse
    {
        public required DateTime StartTime { get; set; }
        public required DateTime EndTime { get; set; }
        public required bool IsAvailable { get; set; }
    }
}
