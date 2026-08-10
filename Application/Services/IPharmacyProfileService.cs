using Application.DTOs.Pharmacy.Request;
using Application.DTOs.Pharmacy.Responses;

namespace Application.Services
{
    public interface IPharmacyProfileService
    {
        Task<Result<PharmacyProfileResponseDto>> GetProfileAsync(
            Guid pharmacyId, CancellationToken cancellationToken = default);

        Task<Result<PharmacyProfileResponseDto>> UpdateProfileAsync(
            Guid pharmacyId,
            UpdatePharmacyProfileDto dto,
            CancellationToken cancellationToken = default);
    }
}
