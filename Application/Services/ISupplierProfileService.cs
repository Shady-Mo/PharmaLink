using Application.DTOs.Supplier;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services
{
    public interface ISupplierProfileService
    {
        Task<Result<SupplierProfileDto>> GetProfileAsync(Guid supplierId);
        Task<Result> UpdateProfileAsync(Guid supplierId, UpdateSupplierProfileDto dto);
    }
}
