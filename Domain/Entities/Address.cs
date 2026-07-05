namespace Domain.Entities;

public class Address {
    public Guid AddressID { get; set; }
    public Guid UserID { get; set; }
    public string Label { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Governorate { get; set; } = string.Empty;
    public NetTopologySuite.Geometries.Point? GeoLocation { get; set; }
    public bool IsDefault { get; set; }

    public Patient Patient { get; set; } = null!;
    public ICollection<Order> Deliveries { get; set; } = [];
}
