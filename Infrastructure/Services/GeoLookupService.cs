namespace Infrastructure.Services;

public class GeoLookupService(AppDbContext context, ILogger<GeoLookupService> logger) : IGeoLookupService
{
    private static readonly IReadOnlyList<double> ExpansionStepsKm = new List<double> { 10.0, 20.0 };

    public async Task<List<NearbyBranchResult>> FindNearbyBranchesAsync(
        Point patientLocation,
        double initialRadiusKm = 5.0,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var radiusSteps = GetRadiusSteps(initialRadiusKm);

        foreach (var radiusKm in radiusSteps)
        {
            var results = await QueryBranchesWithinRadiusAsync(
                patientLocation, radiusKm, cancellationToken);

            if (results.Count > 0)
            {
                logger.LogInformation(
                    "Found {Count} branches within {RadiusKm}km.", results.Count, radiusKm);
                return results;
            }

            logger.LogInformation(
                "No branches found within {RadiusKm}km, expanding search radius.", radiusKm);
        }

        logger.LogWarning(
            "No branches found even after expanding to the widest radius ({MaxRadiusKm}km).",
            radiusSteps[^1]);

        return [];
    }

    private List<double> GetRadiusSteps(double initialRadiusKm)
    {
        return new[] { initialRadiusKm }
            .Concat(ExpansionStepsKm.Where(step => step > initialRadiusKm))
            .ToList();
    }

    private async Task<List<NearbyBranchResult>> QueryBranchesWithinRadiusAsync(
        Point patientLocation,
        double radiusKm,
        CancellationToken cancellationToken)
    {
        var radiusMeters = radiusKm * 1000.0;
        return await (
                from b in context.PharmacyBranches
                let distanceMeters = b.GeoLocation!.Distance(patientLocation)
                where b.GeoLocation != null
                      && distanceMeters <= radiusMeters
                      && distanceMeters <= (double)b.ServiceRadiusKm * 1000.0
                orderby distanceMeters
                select new NearbyBranchResult
                {
                    BranchID = b.BranchId,
                    BranchName = b.BranchName,
                    DistanceKm = distanceMeters / 1000.0,
                    SupportsDelivery = b.SupportsDelivery,
                    SupportsPickup = b.SupportsPickup
                })
            .ToListAsync(cancellationToken);
    }
}