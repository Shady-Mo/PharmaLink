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

        var systemAdmin = await GetOrCreateUserAsync(
            "systemadmin@dev.test", "System Admin", "01000000003", AppRoles.Admin, cancellationToken);

        var pharmacyAdmin = await GetOrCreateUserAsync(
            "pharmacyadmin@dev.test", "Pharmacy Admin", "01000000004", AppRoles.PharmacyAdmin, cancellationToken);

        var patientTokenResult = authService.GenerateTokenForUserAsync(patient, AppRoles.Patient, cancellationToken);

        var pharmacistTokenResult = authService.GenerateTokenForUserAsync(pharmacist, AppRoles.Pharmacist, cancellationToken);

        var systemAdminTokenResult = authService.GenerateTokenForUserAsync(systemAdmin, AppRoles.Admin, cancellationToken);

        var pharmacyAdminTokenResult = authService.GenerateTokenForUserAsync(pharmacyAdmin, AppRoles.PharmacyAdmin, cancellationToken);

        await Task.WhenAll(patientTokenResult, pharmacistTokenResult, systemAdminTokenResult, pharmacyAdminTokenResult);

        return Ok(new
        {
            patient = new
            {
                userId = patient.Id,
                fullName = patient.FullName,
                email = patient.Email,
                role = AppRoles.Patient,
                token = patientTokenResult.Result.IsSuccess ? patientTokenResult.Result.Value.AccessToken : null
            },
            pharmacist = new
            {
                userId = pharmacist.Id,
                fullName = pharmacist.FullName,
                email = pharmacist.Email,
                role = AppRoles.Pharmacist,
                token = pharmacistTokenResult.Result.IsSuccess ? pharmacistTokenResult.Result.Value.AccessToken : null
            },
            systemAdmin = new
            {
                userId = systemAdmin.Id,
                fullName = systemAdmin.FullName,
                email = systemAdmin.Email,
                role = AppRoles.Admin,
                token = systemAdminTokenResult.Result.IsSuccess ? systemAdminTokenResult.Result.Value.AccessToken : null
            },
            pharmacyAdmin = new
            {
                userId = pharmacyAdmin.Id,
                fullName = pharmacyAdmin.FullName,
                email = pharmacyAdmin.Email,
                role = AppRoles.PharmacyAdmin,
                token = pharmacyAdminTokenResult.Result.IsSuccess ? pharmacyAdminTokenResult.Result.Value.AccessToken : null
            }
        });
    }

    private async Task<AppUser> GetOrCreateUserAsync(
        string email,
        string fullName,
        string phone,
        string role,
        CancellationToken cancellationToken)
    {
        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser is not null)
            return existingUser;

        AppUser user = role switch
        {
            AppRoles.Patient => new Patient(),
            AppRoles.Pharmacist => new Pharmacist(),
            AppRoles.Admin => new SystemAdmin(),
            AppRoles.PharmacyAdmin => new PharmacyAdmin(),
            _ => throw new InvalidOperationException($"Unsupported role: {role}")
        };

        user.UserName = email;
        user.Email = email;
        user.FullName = fullName;
        user.PhoneNumber = phone;
        user.PhoneNumberConfirmed = true;
        user.EmailConfirmed = true;

        var createResult = await userManager.CreateAsync(user, "TestP@ssw0rd!");

        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(
                string.Join(Environment.NewLine, createResult.Errors.Select(e => e.Description)));
        }

        var roleResult = await userManager.AddToRoleAsync(user, role);

        if (!roleResult.Succeeded)
        {
            throw new InvalidOperationException(
                string.Join(Environment.NewLine, roleResult.Errors.Select(e => e.Description)));
        }

        if (role == AppRoles.Pharmacist)
        {
            await SeedPharmacyForUserAsync(user.Id, cancellationToken);
        }

        return user;
    }

    private async Task SeedPharmacyForUserAsync(
        Guid pharmacistId,
        CancellationToken cancellationToken)
    {
        var pharmacy = new Pharmacy
        {
            PharmacyId = Guid.NewGuid(),
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