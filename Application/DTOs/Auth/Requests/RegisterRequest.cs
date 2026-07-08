namespace Application.DTOs.Auth.Requests;

/// <summary>
/// The request body for patient self-registration.
/// </summary>
/// <remarks>
/// <b>Security:</b> RoleID / RoleName are intentionally excluded from this contract.
/// The server always hard-codes the role to <c>Patient</c>, preventing privilege escalation.
/// </remarks>
public class RegisterRequestDTO
{
    /// <summary>Full display name of the patient.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Unique email address used as the login credential.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Unique phone number for the account.</summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// Plain-text password supplied by the patient.
    /// It will be hashed by BCrypt before persistence and is never stored in plain-text.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>Must match <see cref="Password"/> exactly.</summary>
    public string ConfirmPassword { get; set; } = string.Empty;
}
