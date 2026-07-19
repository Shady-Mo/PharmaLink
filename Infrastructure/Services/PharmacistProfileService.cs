namespace Infrastructure.Services
{
    public class PharmacistProfileService
        (AppDbContext context,
        ILogger<PharmacistProfileService> logger)
        : IPharmacistProfileService
    {
        public async Task<Result<GetPharmacyProfileResponseDTO>> GetByIdAsync(Guid guid, CancellationToken cancellationToken)
        {
            var pharmacist = await context.Pharmacists.FirstOrDefaultAsync(p => p.Id == guid, cancellationToken);

            var result = pharmacist.Adapt<GetPharmacyProfileResponseDTO>();

            return Result.Success(result);
        }

        public async Task<Result<UpdatePharmacyProfileResponseDTO>> UpdateAsync(Guid guid, UpdatePharmacistProfileRequestDTO updatePharmacy, CancellationToken cancellationToken)
        {
            var existingByPhone = await context.AppUsers
                .FirstOrDefaultAsync(p => p.PhoneNumber == updatePharmacy.PhoneNumber, cancellationToken);
            if (existingByPhone is not null)
            {
                logger.LogWarning("Pharmasict tried to update his profile with existing phone: {Phone}", updatePharmacy.PhoneNumber);
                return Result.Failure<UpdatePharmacyProfileResponseDTO>(PharmacistErrors.PhoneAlreadyExists);
            }

            var pharmacist = await context.Pharmacists.FirstOrDefaultAsync(p => p.Id == guid, cancellationToken);

            if(pharmacist is null)
                return Result.Failure<UpdatePharmacyProfileResponseDTO>(PharmacistErrors.PharmacistNotFound);

            pharmacist.FullName = updatePharmacy.FullName;

            pharmacist.PhoneNumber = updatePharmacy.PhoneNumber;

            context.Update(pharmacist);
            await context.SaveChangesAsync(cancellationToken);

            var result = pharmacist.Adapt<UpdatePharmacyProfileResponseDTO>();

            return Result.Success(result);
        }
    }
}
