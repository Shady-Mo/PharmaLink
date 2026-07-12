using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Identity;
using NetTopologySuite.Geometries;

namespace API.Controllers;

public class AaTestController(
    UserManager<AppUser> userManager,
    IAuthService authService,
    AppDbContext dbContext) : BaseApiController
{
    [HttpGet("test-tokens")]
    public async Task<IActionResult> GetTestTokens(CancellationToken cancellationToken)
    {
        var patient = await GetOrCreateUserAsync(
            "patient@dev.test", "Test Patient", "01000000001", AppRoles.Patient, cancellationToken);

        var pharmacist = await GetOrCreateUserAsync(
            "pharmacist@dev.test", "Test Pharmacist", "01000000002", AppRoles.Pharmacist, cancellationToken);

        var admin = await GetOrCreateUserAsync(
            "admin@dev.test", "System Admin", "01000000003", AppRoles.Admin, cancellationToken);

        var patientTokenResult =
            await authService.GenerateTokenForUserAsync(patient, AppRoles.Patient, cancellationToken);

        var pharmacistTokenResult =
            await authService.GenerateTokenForUserAsync(pharmacist, AppRoles.Pharmacist, cancellationToken);
        var adminTokenResult = await authService.GenerateTokenForUserAsync(admin, AppRoles.Admin, cancellationToken);

        return Ok(new
        {
            patient = new
            {
                userId = patient.Id,
                fullName = patient.FullName,
                email = patient.Email,
                role = AppRoles.Patient,
                token = patientTokenResult.IsSuccess ? patientTokenResult.Value.AccessToken : null
            },
            pharmacist = new
            {
                userId = pharmacist.Id,
                fullName = pharmacist.FullName,
                email = pharmacist.Email,
                role = AppRoles.Pharmacist,
                token = pharmacistTokenResult.IsSuccess ? pharmacistTokenResult.Value.AccessToken : null
            },
            admin = new
            {
                userId = admin.Id,
                fullName = admin.FullName,
                email = admin.Email,
                role = AppRoles.Admin,
                token = adminTokenResult.IsSuccess ? adminTokenResult.Value.AccessToken : null
            }
        });
    }

    private async Task<AppUser> GetOrCreateUserAsync(
        string email, string fullName, string phone, string role, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is not null)
            return user;

        user = new AppUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            PhoneNumber = phone,
            PhoneNumberConfirmed = true,
            EmailConfirmed = true
        };

        await userManager.CreateAsync(user, "TestP@ssw0rd!");

        await userManager.AddToRoleAsync(user, role);

        if (role == AppRoles.Pharmacist)
        {
            await SeedPharmacyForUserAsync(user.Id, cancellationToken);
        }

        return user;
    }

    private async Task SeedPharmacyForUserAsync(Guid pharmacistId, CancellationToken cancellationToken)
    {
        var pharmacy = new Pharmacy
        {
            PharmacyId = Guid.NewGuid(),
            OwnerUserId = pharmacistId,
            LegalName = "Dev Pharmacy",
            LicenseNumber = "DEV-LIC-1234",
            VerificationStatus = VerificationStatus.Verified
        };

        var branch = new PharmacyBranch
        {
            BranchId = Guid.NewGuid(),
            PharmacyId = pharmacy.PharmacyId,
            BranchName = "Dev Main Branch",
            City = "Cairo",
            Governorate = "Cairo",
            GeoLocation = new Point(31.2357, 30.0444) { SRID = 4326 },
            ServiceRadiusKm = 10,
            SupportsDelivery = true,
            SupportsPickup = true
        };

        dbContext.Pharmacies.Add(pharmacy);
        dbContext.PharmacyBranches.Add(branch);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}