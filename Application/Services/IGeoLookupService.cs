using Application.DTOs.Geo;
using NetTopologySuite.Geometries;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Services
{

    public interface IGeoLookupService
    {
        Task<List<NearbyBranchResult>> FindNearbyBranchesAsync(
            Point patientLocation,
            double initialRadiusKm = 5.0,
            CancellationToken cancellationToken = default);
    }
}