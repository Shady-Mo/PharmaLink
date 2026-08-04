using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace Infrastructure.AI.Plugins;

public sealed class GeoDistancePlugin(ILogger<GeoDistancePlugin> logger)
{
    private const double EarthRadiusKm = 6371.0088;

    public sealed record DistanceResult(double DistanceKm, bool WithinRadius);

    [KernelFunction("calculate_distance_km")]
    [Description(
        "Computes the great-circle (Haversine) distance in kilometres between two geographic " +
        "coordinates — typically the patient's delivery location and a pharmacy branch. " +
        "Use this to compare how far candidate branches are from the patient.")]
    public double CalculateDistanceKm(
        [Description("Latitude of the origin point (patient), in decimal degrees.")]
        double originLatitude,
        [Description("Longitude of the origin point (patient), in decimal degrees.")]
        double originLongitude,
        [Description("Latitude of the destination point (pharmacy branch), in decimal degrees.")]
        double destinationLatitude,
        [Description("Longitude of the destination point (pharmacy branch), in decimal degrees.")]
        double destinationLongitude)
    {
        var distance = Haversine(originLatitude, originLongitude, destinationLatitude, destinationLongitude);
        logger.LogDebug(
            "GeoDistancePlugin computed {DistanceKm:F3} km between ({OLat},{OLng}) and ({DLat},{DLng})",
            distance, originLatitude, originLongitude, destinationLatitude, destinationLongitude);
        return Math.Round(distance, 3);
    }

    [KernelFunction("is_within_service_radius")]
    [Description(
        "Determines whether a destination (patient) falls inside a pharmacy branch's delivery " +
        "service radius. Returns the computed distance and a boolean feasibility flag.")]
    public DistanceResult IsWithinServiceRadius(
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
        var distance = Math.Round(
            Haversine(patientLatitude, patientLongitude, branchLatitude, branchLongitude), 3);
        return new DistanceResult(distance, distance <= serviceRadiusKm);
    }

    public static double Haversine(double lat1, double lon1, double lat2, double lon2)
    {
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var rLat1 = ToRadians(lat1);
        var rLat2 = ToRadians(lat2);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(rLat1) * Math.Cos(rLat2) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusKm * c;
    }

    private static double ToRadians(double degrees) => degrees * (Math.PI / 180.0);
}
