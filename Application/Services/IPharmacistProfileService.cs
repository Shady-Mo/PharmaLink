using Application.DTOs.Pharmacy.Request;
using Application.DTOs.Pharmacy.Responses;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services
{
    public interface IPharmacistProfileService
    {
        Task<Result<GetPharmacyProfileResponseDTO>> GetByIdAsync(Guid guid, CancellationToken cancellationToken);
        Task<Result<UpdatePharmacyProfileResponseDTO>> UpdateAsync(Guid guid, UpdatePharmacistProfileRequestDTO updatePharmacy, CancellationToken cancellationToken);
    }
}
