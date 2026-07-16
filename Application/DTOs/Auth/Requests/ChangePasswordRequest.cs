namespace Application.DTOs.Auth.Requests;

/// <summary>
/// Request body for changing a user's password.
/// </summary>
/// <remarks>
/// Requires authentication. The current password must be validated before the new password is applied.
/// The new password must comply with the password policy.
/// </remarks>
public class ChangePasswordRequestDTO
{
    /// <summary>
    /// The current password to validate the user's identity.
    /// </summary>
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>
    /// The new password for the user account.
    /// Must comply with password policy: minimum 8 characters, uppercase, lowercase, digit, and special character.
    /// </summary>
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// Confirmation of the new password. Must match <see cref="NewPassword"/> exactly.
    /// </summary>
    public string ConfirmNewPassword { get; set; } = string.Empty;
}