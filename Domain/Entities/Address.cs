namespace Domain.Entities;

public class Address
{
    public Guid AddressId { get; set; }
    public Guid UserId { get; set; }
    public string AddressLine { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Governorate { get; set; } = string.Empty;
    
    // Extended optional fields
    public string? Label { get; set; }
    public string? BuildingNumber { get; set; }
    public string? FloorNumber { get; set; }
    public string? ApartmentNumber { get; set; }
    public string? AdditionalInstructions { get; set; }
    
    public Point GeoLocation { get; set; }
    
    public bool IsDefault { get; set; }

    public Patient Patient { get; set; } = null!;
    public ICollection<Order> Deliveries { get; set; } = [];
}