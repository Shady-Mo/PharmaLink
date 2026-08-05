using System.Globalization;
using System.Text.Json;
using Application.DTOs.Routing;

namespace Infrastructure.Services;

/// <summary>
/// Service for calculating driving distance and duration using the public OSRM (Open Source Routing Machine) API.
/// OSRM is the single source of truth for distance — no straight-line approximation is ever used.
/// </summary>
public interface IOsrmRoutingService
{
    /// <summary>
    /// Calculates driving distance (km) and duration (minutes) between two geographic coordinates using OSRM.
    /// </summary>
    /// <param name="startLat">Origin latitude in decimal degrees.</param>
    /// <param name="startLon">Origin longitude in decimal degrees.</param>
    /// <param name="destLat">Destination latitude in decimal degrees.</param>
    /// <param name="destLon">Destination longitude in decimal degrees.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// An <see cref="OsrmRouteResultDto"/> containing distance, duration, and success status.
    /// If OSRM fails, <see cref="OsrmRouteResultDto.IsSuccess"/> is false and distance/duration are 0.
    /// </returns>
    Task<OsrmRouteResultDto> GetDrivingRouteAsync(
        double startLat,
        double startLon,
        double destLat,
        double destLon,
        CancellationToken cancellationToken = default);
}

public sealed class OsrmRoutingService(
    IHttpClientFactory httpClientFactory,
    ILogger<OsrmRoutingService> logger) : IOsrmRoutingService
{
    private const string HttpClientName = "OsrmClient";

    public async Task<OsrmRouteResultDto> GetDrivingRouteAsync(
        double startLat,
        double startLon,
        double destLat,
        double destLon,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // CRITICAL: OSRM requires {longitude},{latitude} order (longitude first).
            // Use InvariantCulture to ensure decimal points (not commas) regardless of system locale.
            var startLonStr = startLon.ToString("F6", CultureInfo.InvariantCulture);
            var startLatStr = startLat.ToString("F6", CultureInfo.InvariantCulture);
            var destLonStr = destLon.ToString("F6", CultureInfo.InvariantCulture);
            var destLatStr = destLat.ToString("F6", CultureInfo.InvariantCulture);

            var url = $"https://router.project-osrm.org/route/v1/driving/{startLonStr},{startLatStr};{destLonStr},{destLatStr}?overview=false";

            var client = httpClientFactory.CreateClient(HttpClientName);
            var response = await client.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "OSRM API returned {StatusCode} for route ({StartLat},{StartLon}) -> ({DestLat},{DestLon}).",
                    response.StatusCode, startLat, startLon, destLat, destLon);
                return Failure($"OSRM returned {response.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var osrmResponse = JsonSerializer.Deserialize<OsrmResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (osrmResponse?.Routes == null || osrmResponse.Routes.Count == 0)
            {
                logger.LogWarning(
                    "OSRM API returned no routes for ({StartLat},{StartLon}) -> ({DestLat},{DestLon}).",
                    startLat, startLon, destLat, destLon);
                return Failure("OSRM returned no routes");
            }

            var route = osrmResponse.Routes[0];
            // OSRM returns distance in meters and duration in seconds.
            var distanceKm = Math.Round(route.Distance / 1000.0, 3);
            var durationMinutes = Math.Round(route.Duration / 60.0, 2);

            logger.LogDebug(
                "OSRM route calculated: {DistanceKm} km, {DurationMin} min for ({StartLat},{StartLon}) -> ({DestLat},{DestLon})",
                distanceKm, durationMinutes, startLat, startLon, destLat, destLon);

            return new OsrmRouteResultDto
            {
                DistanceKm = distanceKm,
                DurationMinutes = durationMinutes,
                IsSuccess = true,
                Message = "OSRM route calculated successfully"
            };
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex,
                "OSRM API request failed for ({StartLat},{StartLon}) -> ({DestLat},{DestLon}).",
                startLat, startLon, destLat, destLon);
            return Failure($"HTTP error: {ex.Message}");
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex,
                "OSRM API request timed out for ({StartLat},{StartLon}) -> ({DestLat},{DestLon}).",
                startLat, startLon, destLat, destLon);
            return Failure("OSRM request timed out");
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex,
                "OSRM API response parsing failed for ({StartLat},{StartLon}) -> ({DestLat},{DestLon}).",
                startLat, startLon, destLat, destLon);
            return Failure($"JSON parse error: {ex.Message}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Unexpected error calculating OSRM route for ({StartLat},{StartLon}) -> ({DestLat},{DestLon}).",
                startLat, startLon, destLat, destLon);
            return Failure($"Unexpected error: {ex.Message}");
        }
    }

    private static OsrmRouteResultDto Failure(string reason) => new()
    {
        DistanceKm = 0,
        DurationMinutes = 0,
        IsSuccess = false,
        Message = $"OSRM routing failed: {reason}"
    };

    // OSRM API Response Models
    private sealed class OsrmResponse
    {
        public List<OsrmRoute>? Routes { get; set; }
    }

    private sealed class OsrmRoute
    {
        /// <summary>Distance in meters.</summary>
        public double Distance { get; set; }

        /// <summary>Duration in seconds.</summary>
        public double Duration { get; set; }
    }
}
