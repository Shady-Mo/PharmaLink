using Microsoft.AspNetCore.WebUtilities;

namespace Infrastructure.Services;

/// <summary>
/// Implements patient self-registration using ASP.NET Core Identity.
/// </summary>
/// <remarks>
/// Security guarantees enforced here:
/// <list type="bullet">
///   <item>Role is ALWAYS hard-coded to <c>Patient</c> — never read from the incoming request.</item>
///   <item>Password is hashed by <see cref="UserManager{TUser}"/> (ASP.NET Identity default: BCrypt-based PBKDF2). It is never stored or logged in plain-text.</item>
///   <item><c>EmailConfirmed</c> and <c>PhoneNumberConfirmed</c> default to <c>false</c> per Identity convention.</item>
///   <item>Uniqueness of email AND phone is checked before user creation to return precise 409 errors.</item>
/// </list>
/// </remarks>
public class AuthService(
    UserManager<AppUser> userManager,
    IJwtTokenGeneratorService tokenGenerator,
    AppDbContext dbContext,
    IEmailService emailService,
    ILogger<AuthService> logger) : IAuthService
{
    public async Task<Result<RegisterResponseDTO>> RegisterPatientAsync(
        RegisterRequestDTO request,
        CancellationToken cancellationToken = default)
    {
        var existingUserProp = await userManager.Users
            .Where(u => u.Email == request.Email || u.PhoneNumber == request.PhoneNumber)
            .Select(u => new { u.Email, u.PhoneNumber })
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (existingUserProp is not null)
        {
            if (existingUserProp.Email == request.Email)
            {
                logger.LogWarning("Attempted registration with existing email: {Email}", request.Email);

                return Result.Failure<RegisterResponseDTO>(AuthErrors.EmailAlreadyExists);
            }

            if (existingUserProp.PhoneNumber == request.PhoneNumber)
            {
                logger.LogWarning("Attempted registration with existing phone number: {PhoneNumber}",
                    request.PhoneNumber);

                return Result.Failure<RegisterResponseDTO>(AuthErrors.PhoneAlreadyExists);
            }
        }

        var patient = request.Adapt<Patient>();

        var createResult = await userManager.CreateAsync(patient, request.Password);

        if (!createResult.Succeeded)
        {
            var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));

            logger.LogError("Identity failed to create patient account. Errors: {Errors}", errors);

            return Result.Failure<RegisterResponseDTO>(AuthErrors.RegistrationFailed);
        }

        var roleResult = await userManager.AddToRoleAsync(patient, AppRoles.Patient);

        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(patient);

            var errors = string.Join("; ", roleResult.Errors.Select(e => e.Description));

            logger.LogError("Failed to assign Patient role to user '{UserId}'. Rolled back. Errors: {Errors}",
                patient.Id, errors);

            return Result.Failure<RegisterResponseDTO>(AuthErrors.RegistrationFailed);
        }

        logger.LogInformation("Patient account created successfully. UserId: {UserId}", patient.Id);

        return Result.Success(new RegisterResponseDTO { UserId = patient.Id });
    }


    public async Task<Result<LoginResponseDTO>> LoginAsync(
        LoginRequestDTO request,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim().ToLowerInvariant());

        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            logger.LogWarning("Failed login attempt for {Email}", request.Email);
            return Result.Failure<LoginResponseDTO>(AuthErrors.InvalidCredentials);
        }

        if (user.Status == UserStatus.Suspended)
        {
            logger.LogWarning("Login attempt on suspended account {UserId}", user.Id);
            return Result.Failure<LoginResponseDTO>(AuthErrors.AccountSuspended);
        }

        var roles = await userManager.GetRolesAsync(user);
        var roleName = roles.FirstOrDefault();

        if (roleName is null)
        {
            logger.LogError("User {UserId} has no assigned role.", user.Id);
            return Result.Failure<LoginResponseDTO>(AuthErrors.InvalidCredentials);
        }

        if (roleName == AppRoles.Patient && !user.PhoneNumberConfirmed)
        {
            logger.LogWarning(
                "Login blocked — phone not verified. UserId: {UserId}", user.Id);
            return Result.Failure<LoginResponseDTO>(AuthErrors.PhoneNotVerified);
        }

        return await GenerateTokenForUserAsync(user, roleName, cancellationToken);
    }

    public async Task<Result<LoginResponseDTO>> GenerateTokenForUserAsync(
        AppUser user,
        string roleName,
        CancellationToken cancellationToken = default)
    {
        var claims = new List<Claim>
        {
            new(JwtClaimTypes.UserId, user.Id.ToString()),
            new(JwtClaimTypes.RoleName, roleName),
        };

        switch (roleName)
        {
            case AppRoles.Pharmacist:
                await AddPharmacistClaimsAsync(user.Id, claims, cancellationToken);
                break;

            case AppRoles.Admin:
                claims.Add(new Claim(JwtClaimTypes.Scope, JwtClaimTypes.PlatformScope));
                break;

            case AppRoles.Patient:
                // No branch/platform claims — scope is implicitly the Patient's own UserID.
                break;
        }

        var (token, expiresAtUtc) = tokenGenerator.GenerateToken(claims);

        logger.LogInformation("User {UserId} logged in as {Role}", user.Id, roleName);

        return Result.Success(new LoginResponseDTO
        {
            FullName = user.FullName,
            Email = user.Email,
            UserId = user.Id,
            AccessToken = token,
            ExpiresAtUtc = expiresAtUtc,
            RoleName = roleName
        });
    }

    private async Task AddPharmacistClaimsAsync(
        Guid pharmacistId,
        List<Claim> claims,
        CancellationToken cancellationToken)
    {
        var ownedPharmacies = await dbContext.Pharmacies
            .Where(p => p.OwnerUserId == pharmacistId
                        && p.VerificationStatus == VerificationStatus.Verified)
            .Select(p => new
            {
                p.PharmacyId,
                BranchIds = p.Branches.Select(b => b.BranchId)
            })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        foreach (var pharmacy in ownedPharmacies)
        {
            claims.Add(new Claim(JwtClaimTypes.PharmacyId, pharmacy.PharmacyId.ToString()));

            claims.AddRange(
                pharmacy.BranchIds.Select(branchId => new Claim(JwtClaimTypes.BranchId, branchId.ToString())));
        }

        // Edge case: a Pharmacist account with zero verified pharmacies still gets
        // a valid token (role = Pharmacist) but no PharmacyID/BranchID claims,
        // so every branch-ownership check downstream will correctly reject them
        // until they have at least one verified pharmacy.
    }

    public async Task<Result> ForgotPassword(string email, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(email);

        if(user is null)
            return Result.Failure<LoginResponseDTO>(AuthErrors.InvalidEmail);

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        var resetLink = $"http://localhost:4200/reset-password?email={email}&token={encodedToken}";

        await emailService.SendEmailAsync(email, "Reset Password", $"Click here to reset your password: {resetLink}");

        return Result.Success();
    }

    public async Task<Result> ResetPassword(ResetPasswordDTO resetPasswordDTO, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(resetPasswordDTO.Email);

        if (user is null)
            return Result.Failure<LoginResponseDTO>(AuthErrors.InvalidEmail);

        var decodedTokenBytes = WebEncoders.Base64UrlDecode(resetPasswordDTO.Token);
        var decodedToken = Encoding.UTF8.GetString(decodedTokenBytes);

        var result = await userManager.ResetPasswordAsync(user, decodedToken, resetPasswordDTO.Password);

        if (result.Succeeded)
        {
            return Result.SuccessWithValue("Password has been reset successfully");
        }

        return Result.Failure(AuthErrors.TokenError);

    }
}