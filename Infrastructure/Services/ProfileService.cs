using Application.DTOs.Pharmacy.Responses;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Services
{
    public class ProfileService(AppDbContext context)
        : IProfileService
    {
        public async Task<Result<GetPharmacyProfileResponseDTO>> GetByIdAsync(Guid guid, CancellationToken cancellationToken)
        {
            var pharmaciest = await context.Pharmacists.Include(p => p.AdministeredPharmacies).FirstOrDefaultAsync(p => p.Id == guid, cancellationToken);

            var result = pharmaciest.Adapt<GetPharmacyProfileResponseDTO>();

            return Result.Success(result);
        }
    }
}
