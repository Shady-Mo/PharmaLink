using Application.DTOs.Pharmacy.Responses;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services
{
    public interface IProfileService
    {
        Task<Result<GetPharmacyProfileResponseDTO>> GetByIdAsync(Guid guid, CancellationToken cancellationToken);
    }
}
