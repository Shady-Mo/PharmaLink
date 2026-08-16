using System.Globalization;
using System.Text.Json;
using Application.DTOs.Routing;

namespace Infrastructure.Services;

public interface IOsrmRoutingService
{
    Task<OsrmRouteResultDto> GetDrivingRouteAsync(
        double startLat,
        double startLon,
        double destLat,
        double destLon,
        CancellationToken cancellationToken = default);

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

        if (coordinates.Count == 1)
        {
            return new OsrmMatrixResultDto
            {
                DistancesKm = new[] { new[] { 0.0 } },
                DurationsMinutes = new[] { new[] { 0.0 } },
                IsSuccess = true,
                Message = "Single coordinate supplied, distance is 0."
            };
        }

        try
        {
            var coordList = string.Join(';', coordinates.Select(c =>
                $"{c.Lon.ToString("F6", CultureInfo.InvariantCulture)},{c.Lat.ToString("F6", CultureInfo.InvariantCulture)}"));

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

    private sealed class OsrmResponse
    {
        public List<OsrmRoute>? Routes { get; set; }
    }

    private sealed class OsrmRoute
    {
        public double Distance { get; set; }

        public double Duration { get; set; }
    }

    private sealed class OsrmTableResponse
    {
        public List<List<double?>>? Distances { get; set; }

        public List<List<double?>>? Durations { get; set; }
    }
}



