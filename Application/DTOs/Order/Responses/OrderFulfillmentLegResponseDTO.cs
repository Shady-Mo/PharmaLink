using Domain.Enums;

namespace Application.DTOs.Order.Responses;

public class OrderFulfillmentLegResponseDTO
{
    public Guid LegId { get; set; }
    public LegStatus LegStatus { get; set; }
    public LegType LegType { get; set; }
    public DateTime ReadyByEstimate { get; set; }
    
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public Guid PharmacyId { get; set; }
    public string PharmacyName { get; set; } = string.Empty;
    public string PharmacyLogoUrl { get; set; } = string.Empty;
    
    public string BranchAddressLine { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Governorate { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;
    public string WorkingHours { get; set; } = string.Empty;
    public bool IsOpenNow { get; set; }
    
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? DistanceKm { get; set; }
    
    public string GoogleMapsUrl { get; set; } = string.Empty;
    
    public bool SupportsDelivery { get; set; }
    public bool SupportsPickup { get; set; }
    
    public bool IsReady { get; set; }
    public bool IsCompleted { get; set; }
    public int? EstimatedPreparationMinutes { get; set; }
    public string PickupVerificationCode { get; set; } = string.Empty;

    public ICollection<OrderItemResponseDTO> Items { get; set; } = new List<OrderItemResponseDTO>();
}
