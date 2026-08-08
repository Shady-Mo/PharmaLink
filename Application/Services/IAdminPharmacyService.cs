using Application.DTOs.Pharmacy.Responses;

namespace Application.Services.Pharmacy
{
    public interface IAdminPharmacyService
    {
        Task<Result<PaginatedList<AdminPharmacySummaryDTO>>> GetAllPharmaciesAsync(
            GetAdminPharmaciesRequest request,
            CancellationToken cancellationToken = default);

        Task<Result<AdminPharmacyDetailDTO>> GetPharmacyByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default);

        Task<Result<Guid>> CreatePharmacyAsync(
            AdminCreatePharmacyDTO dto,
            CancellationToken cancellationToken = default);

        Task<Result> UpdatePharmacyAsync(
            Guid id,
            AdminUpdatePharmacyDTO dto,
            CancellationToken cancellationToken = default);

        Task<Result> SoftDeletePharmacyAsync(
            Guid id,
            CancellationToken cancellationToken = default);

        Task<Result> ChangePharmacyStatusAsync(
            Guid id,
            VerificationStatus status,
            CancellationToken cancellationToken = default);

        Task<Result> AssignOwnerAsync(
            Guid pharmacyId,
            Guid ownerId,
            CancellationToken cancellationToken = default);
    }
}
