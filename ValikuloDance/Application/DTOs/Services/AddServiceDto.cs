namespace ValikuloDance.Application.DTOs.Services
{
    public class AddServiceDto
    {
        public required Guid Id {  get; set; }
        public required DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public required string Name { get; set; }
        public required bool IsActive { get; set; } = true;
        public required decimal Price { get; set; }
        public required int DurationMinutes { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public string? Description { get; set; }
        public string Format { get; set; } = "Individual";
        public bool IsPackage { get; set; }
        public int? SessionsCount { get; set; }
    }
}
