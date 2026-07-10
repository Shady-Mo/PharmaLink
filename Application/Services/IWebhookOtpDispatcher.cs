namespace Application.Services;

/// <summary>
/// Dispatches a generated OTP to the user's phone number via an external webhook.
/// Defined in Application so Infrastructure implements it without coupling upward.
/// </summary>
public interface IWebhookOtpDispatcher
{
    /// <summary>
    /// Sends an HTTP POST to the configured n8n webhook with the OTP payload.
    /// </summary>
    /// <param name="phoneNumber">The recipient's phone number (E.164 or local format).</param>
    /// <param name="otp">The plain-text 6-digit code to deliver.</param>
    Task DispatchAsync(string phoneNumber, string otp);
}
