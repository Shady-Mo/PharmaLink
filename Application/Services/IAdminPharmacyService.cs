using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Common;
using Application.DTOs;
using Application.DTOs.Pharmacy.Request;
using Application.DTOs.Pharmacy.Responses;
using Domain.Enums;

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
