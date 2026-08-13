using System.Security.Cryptography;

namespace Infrastructure.Services;

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

        if (user.Status != UserStatus.Active)
        {
            logger.LogWarning("Login attempt on deactivated account {UserId}", user.Id);
            return Result.Failure<LoginResponseDTO>(AuthErrors.AccountDeactivated);
        }

        var roles = await userManager.GetRolesAsync(user);
        var roleName = roles.FirstOrDefault();

        if (roleName is null)
        {
            logger.LogError("User {UserId} has no assigned role.", user.Id);
            return Result.Failure<LoginResponseDTO>(AuthErrors.InvalidCredentials);
        }

        if (roleName != AppRoles.Patient || user.PhoneNumberConfirmed)
            return await GenerateTokenForUserAsync(user, roleName, cancellationToken);

        logger.LogWarning(
            "Login blocked — phone not verified. UserId: {UserId}", user.Id);

        return Result.Success(new LoginResponseDTO()
        {
            UserId = user.Id,
            RequiresPhoneVerification = true
        });
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
            case AppRoles.PharmacyAdmin:
                await AddPharmacyAdminClaimsAsync(user.Id, claims, cancellationToken);
                break;

            case AppRoles.Admin:
                claims.Add(new Claim(JwtClaimTypes.Scope, JwtClaimTypes.PlatformScope));
                break;

            case AppRoles.Patient:
                // No branch/platform claims — scope is implicitly the Patient's own UserID.
                break;
        }

        var (token, expiresAtUtc) = tokenGenerator.GenerateToken(claims);

        var refreshToken = new RefreshToken
        {
            Token = GenerateRefreshToken(),
            ExpiresOn = DateTime.UtcNow.AddDays(7),
            CreatedOn = DateTime.UtcNow
        };

        user.RefreshTokens.Add(refreshToken);
        await userManager.UpdateAsync(user);

        logger.LogInformation("User {UserId} logged in as {Role}", user.Id, roleName);

        return Result.Success(new LoginResponseDTO
        {
            FullName = user.FullName,
            Email = user.Email,
            UserId = user.Id,
            AccessToken = token,
            RefreshToken = refreshToken.Token,
            ExpiresAtUtc = expiresAtUtc,
            RoleName = roleName
        });
    }

    private async Task AddPharmacistClaimsAsync(
        Guid pharmacistId,
        List<Claim> claims,
        CancellationToken cancellationToken)
    {
        var assignedPharmacy = await dbContext.PharmacistAssignments
            .AsNoTracking()
            .FirstOrDefaultAsync(pha => pha.PharmacistId == pharmacistId && pha.IsActive, cancellationToken);

        if (assignedPharmacy is not null)
        {
            claims.Add(new Claim(JwtClaimTypes.PharmacyId, assignedPharmacy.PharmacyId.ToString()));

            claims.Add(new Claim(JwtClaimTypes.BranchId, assignedPharmacy.BranchId.ToString()));
        }
    }

    private async Task AddPharmacyAdminClaimsAsync(
        Guid adminId,
        List<Claim> claims,
        CancellationToken cancellationToken)
    {
        var pharmacyData = await dbContext.PharmacyAdmins
            .Where(pa => pa.Id == adminId && pa.PharmacyId != null)
            .Select(pa => new
            {
                PharmacyId = pa.PharmacyId!.Value,
                BranchIds = pa.Pharmacy!.Branches.Select(b => b.BranchId)
            })
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (pharmacyData is not null)
        {
            claims.Add(new Claim(JwtClaimTypes.PharmacyId, pharmacyData.PharmacyId.ToString()));

            claims.AddRange(
                pharmacyData.BranchIds.Select(branchId => new Claim(JwtClaimTypes.BranchId, branchId.ToString())));
        }
    }

    public async Task<Result> ForgotPassword(string email, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
            return Result.Failure<LoginResponseDTO>(AuthErrors.InvalidEmail);

        await emailService.SendEmailAsync(email, "Reset Password",
            $"Click here to reset your password: https://pharma-link-front-end.vercel.app/auth/reset-password?email={email}&token={WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(await userManager.GeneratePasswordResetTokenAsync(user)))}");

        return Result.Success();
    }

    public async Task<Result> ResetPassword(ResetPasswordDTO resetPasswordDto, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(resetPasswordDto.Email);

        if (user is null)
            return Result.Failure<LoginResponseDTO>(AuthErrors.InvalidEmail);

        var decodedTokenBytes = WebEncoders.Base64UrlDecode(resetPasswordDto.Token);
        var decodedToken = Encoding.UTF8.GetString(decodedTokenBytes);

        var result = await userManager.ResetPasswordAsync(user, decodedToken, resetPasswordDto.Password);

        if (result.Succeeded)
        {
            return Result.SuccessWithValue("Password has been reset successfully");
        }

        return Result.Failure(AuthErrors.TokenError);
    }

    public async Task<Result<LoginResponseDTO>> GetRefreshTokenAsync(
        string token,
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var principal = tokenGenerator.GetPrincipalFromExpiredToken(token);

        if (principal == null)
            return Result.Failure<LoginResponseDTO>(AuthErrors.InvalidCredentials);

        var userId = principal.FindFirstValue(JwtClaimTypes.UserId);

        if (userId == null)
            return Result.Failure<LoginResponseDTO>(AuthErrors.InvalidCredentials);

        var user = await dbContext.Users.FindAsync([Guid.Parse(userId)], cancellationToken);

        if (user == null)
            return Result.Failure<LoginResponseDTO>(AuthErrors.InvalidCredentials);

        // Explicitly load RefreshTokens if not loaded
        await dbContext.Entry(user).Collection(u => u.RefreshTokens).LoadAsync(cancellationToken);

        if (!user.RefreshTokens.Any(t => t.Token == refreshToken && t.IsActive))
            return Result.Failure<LoginResponseDTO>(AuthErrors.InvalidCredentials);

        var existingRefreshToken = user.RefreshTokens.First(t => t.Token == refreshToken && t.IsActive);
        existingRefreshToken.RevokedOn = DateTime.UtcNow;

        var roles = await userManager.GetRolesAsync(user);
        var roleName = roles.FirstOrDefault() ?? string.Empty;

        return await GenerateTokenForUserAsync(user, roleName, cancellationToken);
    }

    public async Task<Result> RevokeRefreshTokenAsync(
        string token,
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var principal = tokenGenerator.GetPrincipalFromExpiredToken(token);
        if (principal == null)
            return Result.Failure(AuthErrors.InvalidCredentials);

        var userId = principal.FindFirstValue(JwtClaimTypes.UserId);
        if (userId == null)
            return Result.Failure(AuthErrors.InvalidCredentials);

        var user = await dbContext.Users.FindAsync([Guid.Parse(userId)], cancellationToken);
        if (user == null) return Result.Failure(AuthErrors.UserNotFound);

        await dbContext.Entry(user).Collection(u => u.RefreshTokens).LoadAsync(cancellationToken);

        var tokenToRevoke = user.RefreshTokens.FirstOrDefault(t => t.Token == refreshToken && t.IsActive);
        if (tokenToRevoke != null)
        {
            tokenToRevoke.RevokedOn = DateTime.UtcNow;
            await userManager.UpdateAsync(user);
        }

        return Result.Success();
    }

    public async Task<Result> ChangePasswordAsync(
        Guid userId,
        ChangePasswordRequestDTO request,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            logger.LogWarning("Change password attempt for non-existent user {UserId}", userId);
            return Result.Failure(AuthErrors.UserNotFound);
        }

        var passwordVerificationResult = await userManager.CheckPasswordAsync(user, request.CurrentPassword);

        if (!passwordVerificationResult)
        {
            logger.LogWarning("Failed password change attempt — incorrect current password. UserId: {UserId}", userId);
            return Result.Failure(AuthErrors.CurrentPasswordIncorrect);
        }

        var changeResult = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);

        if (!changeResult.Succeeded)
        {
            var errors = string.Join("; ", changeResult.Errors.Select(e => e.Description));

            logger.LogError("Failed to change password for user {UserId}. Errors: {Errors}", userId, errors);

            return Result.Failure(AuthErrors.PasswordChangeRequired);
        }

        logger.LogInformation("Password changed successfully for user {UserId}", userId);

        return Result.Success();
    }

    private string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}