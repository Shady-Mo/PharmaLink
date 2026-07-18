namespace Application.Services;

public interface IAuthService
{
    /// <summary>
    /// Registers a new patient account.
    /// </summary>
    /// <remarks>
    /// The role is always hard-coded to <c>Patient</c> server-side.
    /// Callers cannot influence role assignment.
    /// </remarks>
    /// <returns>
    /// <see cref="Result{RegisterResponseDTO}"/> with <see cref="RegisterResponseDTO"/> on success,
    /// or a failure result carrying a 409 Conflict error on duplicate email / phone.
    /// </returns>
    Task<Result<RegisterResponseDTO>> RegisterPatientAsync(
        RegisterRequestDTO request,
        CancellationToken cancellationToken = default);

    Task<Result<LoginResponseDTO>> LoginAsync(
        LoginRequestDTO request,
        CancellationToken cancellationToken = default);

    Task<Result<LoginResponseDTO>> GenerateTokenForUserAsync(
        AppUser user,
        string roleName,
        CancellationToken cancellationToken = default);

    Task<Result<LoginResponseDTO>> GetRefreshTokenAsync(
        string token,
        string refreshToken,
        CancellationToken cancellationToken = default);

    Task<Result> RevokeRefreshTokenAsync(
        string token,
        string refreshToken,
        CancellationToken cancellationToken = default);

    Task<Result> ForgotPassword(string email, CancellationToken cancellationToken);
    
    Task<Result> ResetPassword(ResetPasswordDTO resetPasswordDTO, CancellationToken cancellationToken);

    /// <summary>
    /// Changes the password for an authenticated user.
    /// </summary>
    /// <remarks>
    /// Security guarantees:
    /// - The current password is validated before the new password is applied.
    /// - The new password is hashed using ASP.NET Identity's default hashing algorithm (PBKDF2-based).
    /// - If the current password is incorrect, the operation fails with 401 Unauthorized.
    /// - The new password must comply with the password policy.
    /// </remarks>
    /// <param name="userId">The ID of the user changing their password.</param>
    /// <param name="request">The change password request containing current and new passwords.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// <see cref="Result"/> with success on completion, or a failure result with appropriate HTTP status codes.
    /// </returns>
    Task<Result> ChangePasswordAsync(
        Guid userId,
        ChangePasswordRequestDTO request,
        CancellationToken cancellationToken = default);
}