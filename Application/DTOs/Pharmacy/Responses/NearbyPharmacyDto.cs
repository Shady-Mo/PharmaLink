namespace Application.DTOs.Pharmacy.Responses;

/// <summary>
/// Represents a nearby pharmacy branch returned in the patient-facing search results.
/// </summary>
public class NearbyPharmacyDto
{
    /// <summary>Branch unique identifier.</summary>
    public Guid BranchId { get; set; }

    /// <summary>Name of this specific branch.</summary>
    public string BranchName { get; set; } = string.Empty;

    /// <summary>Legal name of the parent pharmacy.</summary>
    public string PharmacyName { get; set; } = string.Empty;

    /// <summary>URL of the pharmacy logo (may be null if not uploaded).</summary>
    public string? LogoUrl { get; set; }

    /// <summary>Street / address line of the branch.</summary>
    public string AddressLine { get; set; } = string.Empty;

    /// <summary>City the branch is located in.</summary>
    public string City { get; set; } = string.Empty;

    /// <summary>Governorate the branch is located in.</summary>
    public string Governorate { get; set; } = string.Empty;

    /// <summary>Contact phone number for this branch.</summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>Distance from the patient's location in kilometres (rounded to 2 d.p.).</summary>
    public double DistanceKm { get; set; }

    /// <summary>Raw working-hours string as stored (e.g. "9:00 AM – 10:00 PM").</summary>
    public string WorkingHours { get; set; } = string.Empty;

    /// <summary>Whether the branch is currently open based on WorkingHours.</summary>
    public bool IsOpen { get; set; }

    /// <summary>Whether this branch supports home delivery.</summary>
    public bool SupportsDelivery { get; set; }

    /// <summary>Whether this branch supports in-store pickup.</summary>
    public bool SupportsPickup { get; set; }

    /// <summary>Latitude coordinate of the branch (for Maps links).</summary>
    public double? Latitude { get; set; }

    /// <summary>Longitude coordinate of the branch (for Maps links).</summary>
    public double? Longitude { get; set; }

    /// <summary>Coverage radius of the branch in kilometres.</summary>
    public decimal ServiceRadiusKm { get; set; }
}
