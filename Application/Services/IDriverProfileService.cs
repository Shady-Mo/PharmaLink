using Application.DTOs.DeliveryDriver;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services
{
    public interface IDriverProfileService
    {
        Task<Result<GetDriverProfileResponseDTO>> GetByIdAsync(Guid driverId, CancellationToken cancellationToken);
        Task<Result<UpdateDriverProfileResponseDTO>> UpdateAsync(Guid driverId, UpdateDriverProfileRequestDTO request, CancellationToken cancellationToken);
    }
}
