using Application.DTOs.Geo;
using Application.Services;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class GeoLookupService : IGeoLookupService
    {
        private static readonly IReadOnlyList<double> ExpansionStepsKm = new List<double> { 10.0, 20.0 };

        private readonly AppDbContext context;
        private readonly ILogger<GeoLookupService> logger;

        public GeoLookupService(AppDbContext context, ILogger<GeoLookupService> logger)
        {
            this.context = context;
            this.logger = logger;
        }

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

            return new List<NearbyBranchResult>();
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
                    BranchID = b.BranchID,
                    BranchName = b.BranchName,
                    DistanceKm = distanceMeters / 1000.0,
                    SupportsDelivery = b.SupportsDelivery,
                    SupportsPickup = b.SupportsPickup
                })
                .ToListAsync(cancellationToken);
        }
    }
}