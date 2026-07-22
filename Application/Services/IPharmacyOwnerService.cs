using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Common;
using Application.DTOs;
using Application.DTOs.PharmacyOwner.Request;
using Application.DTOs.PharmacyOwner.Responses;

namespace Application.Services.PharmacyOwner
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

        Task<Result> AssignOwnerAsync(
            Guid userId,
            Guid pharmacyId,
            CancellationToken cancellationToken = default);
    }
}
