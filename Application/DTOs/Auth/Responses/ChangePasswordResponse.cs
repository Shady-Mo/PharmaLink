namespace Application.DTOs.Auth.Responses;

/// <summary>
/// Response body for password change operation.
/// </summary>
public class ChangePasswordResponseDTO
{
    /// <summary>
    /// Success message confirming password change.
    /// </summary>
    public string Message { get; set; } = "Password changed successfully.";
}