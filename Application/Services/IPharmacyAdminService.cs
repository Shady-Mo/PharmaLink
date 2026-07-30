using Application.DTOs.Pharmacy.Responses;
using Application.DTOs.PharmacyAdmin.Request;
using Application.DTOs.PharmacyAdmin.Response;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services
{
    public interface IPharmacyAdminService
    {
        Task<Result<GetPharmacyAdminProfile>> GetPharmacyAdminProfile(Guid id);
        Task<Result> UpdateAsync(Guid id, UpdatePharmacyAdminProfileDTO profileDTO, CancellationToken cancellationToken);
    }
}
