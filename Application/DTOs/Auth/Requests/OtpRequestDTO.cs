namespace Application.DTOs.Auth.Requests;

/// <summary>Request body for triggering an OTP SMS to the user's registered phone number.</summary>
public class OtpRequestDTO
{
    /// <summary>The ID of the account that needs phone verification.</summary>
    public Guid UserId { get; set; }
}
