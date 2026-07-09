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
}