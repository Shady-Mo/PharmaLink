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
}