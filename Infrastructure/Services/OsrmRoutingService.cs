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

    /// <summary>
    /// Computes a full driving-distance matrix between every pair of the supplied coordinates in a
    /// SINGLE OSRM <c>/table</c> request. This replaces N individual point-to-point calls when many
    /// branch combinations must be compared (e.g. patient + candidate branches).
    /// </summary>
    /// <param name="coordinates">
    /// Ordered list of <c>(latitude, longitude)</c> points. The returned matrix uses this same order:
    /// element <c>[i][j]</c> is the distance from <c>coordinates[i]</c> to <c>coordinates[j]</c>.
    /// Convention: index 0 is typically the patient, indices 1..n the candidate branches.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// An <see cref="OsrmMatrixResultDto"/> with the km distance matrix. On failure
    /// <see cref="OsrmMatrixResultDto.IsSuccess"/> is false and the matrix is empty.
    /// </returns>
    Task<OsrmMatrixResultDto> GetDistanceMatrixAsync(
        IReadOnlyList<(double Lat, double Lon)> coordinates,
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

    public async Task<OsrmMatrixResultDto> GetDistanceMatrixAsync(
        IReadOnlyList<(double Lat, double Lon)> coordinates,
        CancellationToken cancellationToken = default)
    {
        if (coordinates.Count == 0)
            return MatrixFailure("No coordinates supplied");

        try
        {
            // OSRM /table returns an N×N matrix in ONE request. Coordinates are ';'-separated and,
            // as with /route, each is "{longitude},{latitude}" (longitude first), InvariantCulture
            // so decimals never render as commas.
            var coordList = string.Join(';', coordinates.Select(c =>
                $"{c.Lon.ToString("F6", CultureInfo.InvariantCulture)},{c.Lat.ToString("F6", CultureInfo.InvariantCulture)}"));

            // annotations=distance,duration → return BOTH the distance matrix (metres) and the
            // duration matrix (seconds) in the same request, so callers can compute a delivery ETA.
            var url = $"https://router.project-osrm.org/table/v1/driving/{coordList}?annotations=distance,duration";


            var client = httpClientFactory.CreateClient(HttpClientName);
            var response = await client.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("OSRM /table returned {StatusCode} for {Count} coordinates.",
                    response.StatusCode, coordinates.Count);
                return MatrixFailure($"OSRM returned {response.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var table = JsonSerializer.Deserialize<OsrmTableResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (table?.Distances is null || table.Distances.Count == 0)
            {
                logger.LogWarning("OSRM /table returned no distance matrix for {Count} coordinates.", coordinates.Count);
                return MatrixFailure("OSRM returned no distance matrix");
            }

            // Convert metres → km, seconds → minutes. A null cell means OSRM could not route that
            // pair → MaxValue so callers treat it as infeasible rather than "distance/duration 0".
            var distMatrix = table.Distances
                .Select(row => row
                    .Select(cell => cell.HasValue ? Math.Round(cell.Value / 1000.0, 3) : double.MaxValue)
                    .ToArray())
                .ToArray();

            var durMatrix = table.Durations != null
                ? table.Durations
                    .Select(row => row
                        .Select(cell => cell.HasValue ? Math.Round(cell.Value / 60.0, 2) : double.MaxValue)
                        .ToArray())
                    .ToArray()
                : Array.Empty<double[]>();

            logger.LogDebug("OSRM /table computed a {Size}×{Size} distance+duration matrix in one request.", distMatrix.Length, distMatrix.Length);

            return new OsrmMatrixResultDto
            {
                DistancesKm = distMatrix,
                DurationsMinutes = durMatrix,
                IsSuccess = true,
                Message = "OSRM distance+duration matrix calculated successfully"
            };

        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "OSRM /table request failed for {Count} coordinates.", coordinates.Count);
            return MatrixFailure($"HTTP error: {ex.Message}");
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "OSRM /table request timed out for {Count} coordinates.", coordinates.Count);
            return MatrixFailure("OSRM request timed out");
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "OSRM /table response parsing failed for {Count} coordinates.", coordinates.Count);
            return MatrixFailure($"JSON parse error: {ex.Message}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error calculating OSRM /table matrix for {Count} coordinates.", coordinates.Count);
            return MatrixFailure($"Unexpected error: {ex.Message}");
        }
    }

    private static OsrmRouteResultDto Failure(string reason) => new()
    {
        DistanceKm = 0,
        DurationMinutes = 0,
        IsSuccess = false,
        Message = $"OSRM routing failed: {reason}"
    };

    private static OsrmMatrixResultDto MatrixFailure(string reason) => new()
    {
        DistancesKm = [],
        IsSuccess = false,
        Message = $"OSRM matrix routing failed: {reason}"
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

    private sealed class OsrmTableResponse
    {
        /// <summary>N×N distance matrix in metres; a cell is null when the pair is unroutable.</summary>
        public List<List<double?>>? Distances { get; set; }

        /// <summary>N×N duration matrix in seconds; a cell is null when the pair is unroutable.</summary>
        public List<List<double?>>? Durations { get; set; }
    }
}



