namespace Application.DTOs.DeliveryDriver;

public class DeliveryJobNotificationDto
{
    public Guid JobId { get; set; }
    public string PharmacyName { get; set; } = string.Empty;
    public string FullAddress { get; set; } = string.Empty;
    public decimal DeliveryFee { get; set; }
    public double DistanceKm { get; set; }
    public double Longitude { get; set; }
    public double Latitude { get; set; }
    public double PharmacyLatitude { get; set; }
    public double PharmacyLongitude { get; set; }
}