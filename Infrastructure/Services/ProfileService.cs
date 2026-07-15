using Application.DTOs.Pharmacy.Request;
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

        public async Task<Result<UpdatePharmacyProfileResponseDTO>> UpdateAsync(Guid guid, UpdatePharmacyProfileRequestDTO updatePharmacy, CancellationToken cancellationToken)
        {
            var pharmaciest = await context.Pharmacists.FirstOrDefaultAsync(p => p.Id == guid, cancellationToken);

            if(pharmaciest is null)
                return Result.Failure<UpdatePharmacyProfileResponseDTO>(PharmaciestError.PharmaciestNotFound);

            pharmaciest.FullName = updatePharmacy.FullName;

            pharmaciest.PhoneNumber = updatePharmacy.PhoneNumber;

            context.Update(pharmaciest);
            await context.SaveChangesAsync(cancellationToken);

            var result = pharmaciest.Adapt<UpdatePharmacyProfileResponseDTO>();

            return Result.Success(result);
        }
    }
}
