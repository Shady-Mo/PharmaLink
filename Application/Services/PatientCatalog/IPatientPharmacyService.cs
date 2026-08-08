namespace Application.Services.PatientCatalog;

public interface IPatientPharmacyService
{
    /// <summary>
    /// Returns a paginated list of nearby pharmacy branches sorted by distance.
    /// </summary>
    Task<Result<PaginatedList<NearbyPharmacyDto>>> GetNearbyPharmaciesAsync(
        NearbyPharmaciesRequest request,
        CancellationToken cancellationToken = default);
}
