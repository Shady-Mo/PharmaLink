using System.ComponentModel;
using System.Text.Json;
using Infrastructure.Services;
using Microsoft.SemanticKernel;

namespace Infrastructure.AI.Plugins;

public sealed class GeoDistancePlugin(
    IOsrmRoutingService osrmRoutingService,
    ILogger<GeoDistancePlugin> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public sealed record DistanceResult(double DistanceKm, bool WithinRadius);

    private sealed record Stop(double Lat, double Lng);


    [KernelFunction("calculate_distance_km")]
    [Description(
        "Computes the real-world driving distance in kilometres between two geographic " +
        "coordinates — typically the patient's delivery location and a pharmacy branch — using " +
        "the OSRM routing engine. Use this to compare how far candidate branches are from the patient.")]
    public async Task<double> CalculateDistanceKmAsync(
        [Description("Latitude of the origin point (patient), in decimal degrees.")]
        double originLatitude,
        [Description("Longitude of the origin point (patient), in decimal degrees.")]
        double originLongitude,
        [Description("Latitude of the destination point (pharmacy branch), in decimal degrees.")]
        double destinationLatitude,
        [Description("Longitude of the destination point (pharmacy branch), in decimal degrees.")]
        double destinationLongitude)
    {
        var route = await osrmRoutingService.GetDrivingRouteAsync(
            originLatitude, originLongitude, destinationLatitude, destinationLongitude);

        logger.LogDebug(
            "GeoDistancePlugin computed {DistanceKm:F3} km (OSRM success={Success}) between ({OLat},{OLng}) and ({DLat},{DLng})",
            route.DistanceKm, route.IsSuccess, originLatitude, originLongitude, destinationLatitude, destinationLongitude);

        return Math.Round(route.DistanceKm, 3);
    }

    [KernelFunction("calculate_trip_distance_km")]
    [Description(
        "Computes the TOTAL real-world driving distance (km) of a full multi-stop trip that starts " +
        "at the patient and visits each pharmacy branch, in the given order, using the OSRM routing " +
        "engine for every leg (patient→stop1, stop1→stop2, ...). Use this to compare candidate " +
        "routing options and pick the one with the SMALLEST total trip distance. Try the branch " +
        "orderings that make sense (e.g. nearest first) and compare their totals.")]
    public async Task<double> CalculateTripDistanceKmAsync(
        [Description("Patient (start) latitude, in decimal degrees.")]
        double patientLatitude,
        [Description("Patient (start) longitude, in decimal degrees.")]
        double patientLongitude,
        [Description(
            "Ordered JSON array of the pharmacy branch stops to visit, each as {\"lat\":<num>,\"lng\":<num>}. " +
            "The trip is patient → stops[0] → stops[1] → ... in this exact order.")]
        string orderedStopsJson)
    {
        List<Stop>? stops;
        try
        {
            stops = JsonSerializer.Deserialize<List<Stop>>(orderedStopsJson, JsonOptions);
        }
        catch (JsonException)
        {
            logger.LogWarning("GeoDistancePlugin.calculate_trip_distance_km received invalid JSON: {Json}", orderedStopsJson);
            return -1;
        }

        if (stops is null || stops.Count == 0)
            return 0;

        var coords = new List<(double Lat, double Lon)> { (patientLatitude, patientLongitude) };
        coords.AddRange(stops.Select(s => (s.Lat, s.Lng)));

        var matrix = await osrmRoutingService.GetDistanceMatrixAsync(coords);
        if (!matrix.IsSuccess)
        {
            logger.LogWarning(
                "GeoDistancePlugin.calculate_trip_distance_km — OSRM /table failed ({Msg}); trip marked infeasible.",
                matrix.Message);
            return -1;
        }

        double total = 0;
        for (var i = 0; i < coords.Count - 1; i++)
        {
            var legKm = matrix.DistancesKm[i][i + 1];
            if (legKm >= double.MaxValue)
            {
                logger.LogWarning(
                    "GeoDistancePlugin.calculate_trip_distance_km — leg {From}->{To} is unroutable; trip marked infeasible.",
                    i, i + 1);
                return -1;
            }

            total += legKm;
        }

        var rounded = Math.Round(total, 3);
        logger.LogDebug(
            "GeoDistancePlugin.calculate_trip_distance_km — {StopCount} stop(s), total {Km:F3} km (single /table request)",
            stops.Count, rounded);
        return rounded;

    }

    [KernelFunction("is_within_service_radius")]

    [Description(
        "Determines whether a destination (patient) falls inside a pharmacy branch's delivery " +
        "service radius using the real driving distance from the OSRM routing engine. Returns the " +
        "computed distance and a boolean feasibility flag.")]
    public async Task<DistanceResult> IsWithinServiceRadiusAsync(
        [Description("Latitude of the patient, in decimal degrees.")]
        double patientLatitude,
        [Description("Longitude of the patient, in decimal degrees.")]
        double patientLongitude,
        [Description("Latitude of the pharmacy branch, in decimal degrees.")]
        double branchLatitude,
        [Description("Longitude of the pharmacy branch, in decimal degrees.")]
        double branchLongitude,
        [Description("The branch's advertised delivery service radius, in kilometres.")]
        double serviceRadiusKm)
    {
        var route = await osrmRoutingService.GetDrivingRouteAsync(
            patientLatitude, patientLongitude, branchLatitude, branchLongitude);

        var distance = Math.Round(route.DistanceKm, 3);
        return new DistanceResult(distance, distance <= serviceRadiusKm);
    }
}
