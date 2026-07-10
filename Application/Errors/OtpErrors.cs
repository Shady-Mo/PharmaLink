namespace Application.Errors;

public static class OtpErrors
{
    public static readonly Error UserNotFound =
        new("Otp.UserNotFound",
            "No account was found for the provided user ID.",
            StatusCodes.Status404NotFound);

    public static readonly Error PhoneAlreadyVerified =
        new("Otp.PhoneAlreadyVerified",
            "This phone number is already verified.",
            StatusCodes.Status409Conflict);

    public static readonly Error RateLimitExceeded =
        new("Otp.RateLimitExceeded",
            "Too many OTP attempts. Please wait 5 minutes before trying again.",
            StatusCodes.Status429TooManyRequests);

    /// <summary>
    /// Returned for both incorrect code AND expired code.
    /// Using a single error intentionally prevents an attacker from
    /// distinguishing between "wrong code" and "timed out".
    /// </summary>
    public static readonly Error InvalidOrExpired =
        new("Otp.InvalidOrExpired",
            "The OTP code is invalid or has expired.",
            StatusCodes.Status400BadRequest);

    public static readonly Error SmsFailed =
        new("Otp.SmsFailed",
            "Failed to send the verification SMS. Please try again later.",
            StatusCodes.Status503ServiceUnavailable);
}
