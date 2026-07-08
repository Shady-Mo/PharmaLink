namespace Application.DTOs.Auth.Responses;

/// <summary>
/// Returned on successful patient registration (HTTP 201 Created).
/// </summary>
public class RegisterResponseDTO
{
    /// <summary>The newly created user's unique identifier.</summary>
    public Guid UserId { get; set; }
}
