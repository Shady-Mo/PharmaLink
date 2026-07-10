namespace API.Controllers;

/// <summary>
/// Manages phone number verification via OTP for registered patients.
/// </summary>
public class PhoneVerificationController(IOtpService otpService) : BaseApiController
{
    /// <summary>
    /// Sends a 6-digit OTP to the user's registered phone number.
    /// </summary>
    /// <remarks>
    /// **Rate limit:** Max 5 requests per 15-minute window per user.
    /// Issuing a new OTP resets the code and restarts the 5-minute expiry.
    /// </remarks>
    /// <param name="request">The user's ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// **200 OK** — OTP dispatched.
    /// **400 Bad Request** — validation failure.
    /// **404 Not Found** — user does not exist.
    /// **409 Conflict** — phone already verified.
    /// **429 Too Many Requests** — rate limit hit.
    /// **503 Service Unavailable** — Twilio delivery failure.
    /// </returns>
    [HttpPost("request")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> RequestOtp(
        [FromBody] OtpRequestDTO request,
        CancellationToken cancellationToken)
    {
        var result = await otpService.RequestPhoneOtpAsync(request, cancellationToken);

        return result.IsFailure
            ? result.ToProblem()
            : Ok(new { message = $"Verification code sent to your registered phone number {result.Value}." });
    }

    /// <summary>
    /// Verifies the OTP submitted by the user and marks the phone as confirmed on success.
    /// </summary>
    /// <remarks>
    /// **Security:**
    /// - OTP expires in 5 minutes.
    /// - Max 5 attempts per 15-minute window.
    /// - Attempt count is incremented before validation — every guess counts.
    /// - Wrong-code and expired-code responses are identical to prevent oracle attacks.
    /// </remarks>
    /// <param name="request">The user's ID and the 6-digit OTP code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// **200 OK** — phone number verified.
    /// **400 Bad Request** — invalid or expired OTP.
    /// **429 Too Many Requests** — rate limit hit.
    /// </returns>
    [HttpPost("verify")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> VerifyOtp(
        [FromBody] OtpVerifyDTO request,
        CancellationToken cancellationToken)
    {
        var result = await otpService.VerifyPhoneOtpAsync(request, cancellationToken);

        return result.IsFailure
            ? result.ToProblem()
            : Ok(new { message = "Phone number verified successfully." });
    }
}
