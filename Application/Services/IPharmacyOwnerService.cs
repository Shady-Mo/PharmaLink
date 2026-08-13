using Application.DTOs.PharmacyOwner.Request;
using Application.DTOs.PharmacyOwner.Responses;

namespace Application.Services
{
    public interface IPharmacyOwnerService
    {
        Task<Result<PharmacyOwnerResponseDTO>> CreatePharmacyOwnerAsync(
            CreatePharmacyOwnerDTO dto,
            CancellationToken cancellationToken = default);

        Task<Result<PharmacyOwnerResponseDTO>> GetPharmacyOwnerByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default);

        Task<Result<PaginatedList<PharmacyOwnerResponseDTO>>> GetAllPharmacyOwnersAsync(
            GetPharmacyOwnersRequest request,
            CancellationToken cancellationToken = default);

        Task<Result> UpdatePharmacyOwnerAsync(
            Guid id,
            UpdatePharmacyOwnerDTO dto,
            CancellationToken cancellationToken = default);

        Task<Result> SoftDeletePharmacyOwnerAsync(
            Guid id,
            CancellationToken cancellationToken = default);

        Task<Result> ChangePharmacyOwnerStatusAsync(
            Guid id,
            UserStatus status,
            CancellationToken cancellationToken = default);

        Task<Result> AssignOwnerAsync(
            Guid userId,
            Guid pharmacyId,
            CancellationToken cancellationToken = default);
    }
}
