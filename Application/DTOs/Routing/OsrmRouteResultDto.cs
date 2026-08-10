namespace Application.DTOs.Routing;

/// <summary>
/// Response model for OSRM routing API results containing distance and duration.
/// </summary>
public sealed class OsrmRouteResultDto
{
    /// <summary>
    /// Driving distance in kilometers.
    /// </summary>
    public double DistanceKm { get; init; }

    /// <summary>
    /// Estimated driving duration in minutes.
    /// </summary>
    public double DurationMinutes { get; init; }

    /// <summary>
    /// Indicates whether the OSRM API call succeeded.
    /// When false, no route could be resolved and distance/duration are 0.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Optional status message or failure reason.
    /// </summary>
    public string? Message { get; init; }
}
