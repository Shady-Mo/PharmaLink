namespace Domain.Entities;

public class Address
{
    public Guid AddressId { get; set; }
    public Guid UserId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Governorate { get; set; } = string.Empty;
    
    public Point GeoLocation { get; set; }
    
    public bool IsDefault { get; set; }

    public Patient Patient { get; set; } = null!;
    public ICollection<Order> Deliveries { get; set; } = [];
}