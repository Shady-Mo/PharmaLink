using Application.DTOs.PharmacyBranch.Request;
using Application.DTOs.PharmacyBranch.Response;

namespace Application.Services.Pharmacy
{
    public interface IPharmacyBranchService
    {
        Task<Result<GetPharmacyBranchResponseDTO>> CreateAsync(
            Guid pharmacyId,
            CreatePharmacyBranchDTO dto,
            CancellationToken cancellationToken = default);

        Task<Result<PaginatedList<GetPharmacyBranchResponseDTO>>> GetAllAsync(
            Guid pharmacyId,
            GetPharmacyBranchParamRequest parameters,
            CancellationToken cancellationToken = default);

        Task<Result<PharmacyBranchResponseDTO>> GetByIdAsync(
            Guid pharmacyId,
            Guid branchId,
            CancellationToken cancellationToken = default);

        Task<Result<GetPharmacyBranchResponseDTO>> UpdateAsync(
            Guid pharmacyId,
            Guid branchId,
            UpdatePharmacyBranchDTO dto,
            CancellationToken cancellationToken = default);

        Task<Result> DeleteAsync(
            Guid pharmacyId,
            Guid branchId,
            CancellationToken cancellationToken = default);

        Task<Result<List<PharmacyBranchSearchDTO>>> SearchAsync(
            Guid pharmacyId,
            string? term,
            CancellationToken cancellationToken = default);
    }
}
