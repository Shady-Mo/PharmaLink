using Application.DTOs.Geo;
using NetTopologySuite.Geometries;

namespace Application.Services;

public interface IGeoLookupService
{
    Task<List<NearbyBranchResult>> FindNearbyBranchesAsync(
        Point patientLocation,
        double initialRadiusKm = 5.0,
        CancellationToken cancellationToken = default);
}