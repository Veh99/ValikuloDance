namespace ValikuloDance.Domain.Entities
{
    public class Service : BaseEntity
    {
        public required string Name { get; set; }
        public string? Description { get; set; }
        public required decimal Price { get; set; }
        public required int DurationMinutes { get; set; }
        public required string Format { get; set; } = "Individual";
        public bool IsPackage { get; set; }
        public int? SessionsCount { get; set; }
        public string? Icon { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
