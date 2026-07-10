namespace Application.Services;

/// <summary>
/// Handles the full OTP lifecycle: generation, delivery, rate-limit enforcement, and verification.
/// </summary>
public interface IOtpService
{
    /// <summary>
    /// Generates a cryptographically secure 6-digit OTP, persists a BCrypt hash of it,
    /// and dispatches it to the user's registered phone number via Twilio.
    /// </summary>
    Task<Result> RequestPhoneOtpAsync(
        OtpRequestDTO request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the submitted OTP against the stored hash, enforces the 5-minute expiry
    /// and the rolling 15-minute rate-limit window, and sets PhoneNumberConfirmed = true on success.
    /// </summary>
    Task<Result> VerifyPhoneOtpAsync(
        OtpVerifyDTO request,
        CancellationToken cancellationToken = default);
}
