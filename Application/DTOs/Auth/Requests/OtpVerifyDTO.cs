namespace Application.DTOs.Auth.Requests;

/// <summary>Request body for submitting the 6-digit OTP for verification.</summary>
public class OtpVerifyDTO
{
    /// <summary>The ID of the account being verified.</summary>
    public Guid UserId { get; set; }

    /// <summary>The 6-digit numeric code received via SMS.</summary>
    public string Code { get; set; } = string.Empty;
}
