using Application.DTOs.PharmacyAdmin.Request;
using Application.DTOs.PharmacyAdmin.Response;
using DocumentFormat.OpenXml.Presentation;
using System;
using System.Collections.Generic;
using System.Text;
using Twilio.TwiML.Messaging;

namespace Infrastructure.Services
{
    public class PharmacyAdminService(AppDbContext context) : IPharmacyAdminService
    {
        public async Task<Result<GetPharmacyAdminProfile>> GetPharmacyAdminProfile(Guid id)
        {
            var result = context.PharmacyAdmins.Where(a => a.Id == id)
                .Select(a => new GetPharmacyAdminProfile {
                    FullName = a.FullName,
                    Email = a.Email,
                    PhoneNumber = a.PhoneNumber,
                    VerificationStatus = a.Pharmacy.VerificationStatus,
                    LegalName = a.Pharmacy.LegalName,
                    LicenseNumber = a.Pharmacy.LicenseNumber,
                    LogoUrl = a.Pharmacy.LogoUrl
                })
                .FirstOrDefault();

            return Result.Success(result);
        }

        public async Task<Result> UpdateAsync(Guid id, UpdatePharmacyAdminProfileDTO profileDTO, CancellationToken cancellationToken)
        {
            var existingByPhone = await context.AppUsers
                .FirstOrDefaultAsync(p => p.PhoneNumber == profileDTO.PhoneNumber && p.Id != id, cancellationToken);
            if (existingByPhone is not null)
                return Result.Failure(PharmacyAdminErrors.PhoneAlreadyExists);
            

            var pharmacyAdmin = await context.PharmacyAdmins.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

            if (pharmacyAdmin is null)
                return Result.Failure(PharmacyAdminErrors.PharmacistNotFound);

            pharmacyAdmin.FullName = profileDTO.FullName;

            pharmacyAdmin.PhoneNumber = profileDTO.PhoneNumber;

            context.Update(pharmacyAdmin);
            await context.SaveChangesAsync(cancellationToken);


            return Result.SuccessWithValue("Update Successfuly");
        }
    }
}
