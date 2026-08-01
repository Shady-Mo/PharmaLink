namespace Application.DTOs.Pharmacy.Request;

/// <summary>
/// Query parameters for searching nearby pharmacy branches by geolocation.
/// </summary>
public class NearbyPharmaciesRequest : PaginatedRequest
{
    /// <summary>
    /// Patient latitude (-90 to 90).
    /// </summary>
    /// <example>30.0444</example>
    public double Latitude { get; set; }

    /// <summary>
    /// Patient longitude (-180 to 180).
    /// </summary>
    /// <example>31.2357</example>
    public double Longitude { get; set; }

    /// <summary>
    /// Search radius in kilometers (1–50). Defaults to 10.
    /// </summary>
    /// <example>10</example>
    public double RadiusKm { get; set; } = 10;

    /// <summary>
    /// Optional text search on pharmacy or branch name.
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// When true, returns only currently-open branches.
    /// </summary>
    public bool? IsOpen { get; set; }
}
