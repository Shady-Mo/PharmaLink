using System.ComponentModel;
using Infrastructure.Services;
using Microsoft.SemanticKernel;

namespace Infrastructure.AI.Plugins;

public sealed class GeoDistancePlugin(
    IOsrmRoutingService osrmRoutingService,
    ILogger<GeoDistancePlugin> logger)
{
    public sealed record DistanceResult(double DistanceKm, bool WithinRadius);

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
