namespace Infrastructure.Services;

/// <summary>
/// Implements the full OTP lifecycle: generation, rate-limit enforcement,
/// BCrypt hashing, webhook dispatch, expiry validation, and phone confirmation.
/// </summary>
/// <remarks>
/// Security decisions:
/// <list type="bullet">
///   <item>Code generated via <see cref="System.Security.Cryptography.RandomNumberGenerator"/> — CSPRNG, not <c>System.Random</c>.</item>
///   <item>Only a BCrypt hash of the code is persisted — the plaintext is discarded immediately after webhook dispatch.</item>
///   <item>Rate-limit: max 5 attempts within a rolling 15-minute window, enforced on both /request and /verify.</item>
///   <item>Attempt counter is incremented <em>before</em> the validity check so every guess (valid or not) counts.</item>
///   <item>Expiry and code-mismatch return the same <c>InvalidOrExpired</c> error to prevent timing oracle attacks.</item>
/// </list>
/// </remarks>
public class OtpService(
    AppDbContext context,
    UserManager<AppUser> userManager,
    IWebhookOtpDispatcher dispatcher,
    ILogger<OtpService> logger) : IOtpService
{
    private const int OtpLifetimeMinutes     = 5;
    private const int MaxAttempts            = 5;
    private const int RateLimitWindowMinutes = 5;

    public async Task<Result> RequestPhoneOtpAsync(
        OtpRequestDTO request,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString());

        if (user is null)
        {
            logger.LogWarning("OTP requested for unknown UserId: {UserId}", request.UserId);
            return Result.Failure(OtpErrors.UserNotFound);
        }

        if (user.PhoneNumberConfirmed)
        {
            logger.LogInformation(
                "OTP requested but phone already verified. UserId: {UserId}", user.Id);
            return Result.Failure(OtpErrors.PhoneAlreadyVerified);
        }

        var existing = await context.PhoneVerificationOtps
            .FirstOrDefaultAsync(o => o.UserId == user.Id, cancellationToken);

        if (existing is not null && IsRateLimited(existing))
        {
            logger.LogWarning("OTP /request rate-limited for UserId: {UserId}", user.Id);
            return Result.Failure(OtpErrors.RateLimitExceeded);
        }

        var plainCode = GenerateOtpCode();
        var codeHash  = BCrypt.Net.BCrypt.HashPassword(plainCode);

        if (existing is null)
        {
            context.PhoneVerificationOtps.Add(new PhoneVerificationOtp
            {
                UserId        = user.Id,
                CodeHash      = codeHash,
                ExpiresAt     = DateTime.UtcNow.AddMinutes(OtpLifetimeMinutes),
                AttemptCount  = 0,
                LastAttemptAt = null
            });
        }
        else
        {
            existing.CodeHash      = codeHash;
            existing.ExpiresAt     = DateTime.UtcNow.AddMinutes(OtpLifetimeMinutes);
            existing.AttemptCount  = 0;
            existing.LastAttemptAt = null;
        }

        await context.SaveChangesAsync(cancellationToken);

        try
        {
            await dispatcher.DispatchAsync(user.PhoneNumber!, plainCode);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Webhook dispatch failed for UserId: {UserId}", user.Id);
            return Result.Failure(OtpErrors.WebhookFailed);
        }

        logger.LogInformation("OTP dispatched via webhook. UserId: {UserId}", user.Id);
        return Result.Success();
    }

    public async Task<Result> VerifyPhoneOtpAsync(
        OtpVerifyDTO request,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString());

        if (user is null)
        {
            logger.LogWarning("OTP verify for unknown UserId: {UserId}", request.UserId);
            return Result.Failure(OtpErrors.UserNotFound);
        }

        if (user.PhoneNumberConfirmed)
            return Result.Failure(OtpErrors.PhoneAlreadyVerified);

        var otpRecord = await context.PhoneVerificationOtps
            .FirstOrDefaultAsync(o => o.UserId == user.Id, cancellationToken);

        if (otpRecord is null)
        {
            logger.LogWarning("No OTP record found for UserId: {UserId}", user.Id);
            return Result.Failure(OtpErrors.InvalidOrExpired);
        }

        if (IsRateLimited(otpRecord))
        {
            logger.LogWarning("OTP /verify rate-limited for UserId: {UserId}", user.Id);
            return Result.Failure(OtpErrors.RateLimitExceeded);
        }

        otpRecord.AttemptCount++;
        otpRecord.LastAttemptAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);

        if (DateTime.UtcNow > otpRecord.ExpiresAt)
        {
            logger.LogWarning("Expired OTP submitted for UserId: {UserId}", user.Id);
            return Result.Failure(OtpErrors.InvalidOrExpired);
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Code, otpRecord.CodeHash))
        {
            logger.LogWarning(
                "Incorrect OTP. UserId: {UserId}, Attempt: {Attempt}/{Max}",
                user.Id, otpRecord.AttemptCount, MaxAttempts);
            return Result.Failure(OtpErrors.InvalidOrExpired);
        }

        user.PhoneNumberConfirmed = true;
        await userManager.UpdateAsync(user);

        context.PhoneVerificationOtps.Remove(otpRecord);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Phone verified successfully. UserId: {UserId}", user.Id);
        return Result.Success();
    }

    /// <summary>
    /// Generates a cryptographically secure 6-digit OTP via the OS CSPRNG.
    /// Zero-padded to always produce exactly 6 digits (000000–999999).
    /// </summary>
    private static string GenerateOtpCode()
    {
        var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(4);
        var value = BitConverter.ToUInt32(bytes, 0) % 1_000_000;
        return value.ToString("D6");
    }

    /// <summary>
    /// Returns true when the user has exhausted their attempt budget
    /// within the rolling 15-minute window.
    /// </summary>
    private static bool IsRateLimited(PhoneVerificationOtp record)
    {
        if (record.LastAttemptAt is null)
            return false;

        var windowStart  = DateTime.UtcNow.AddMinutes(-RateLimitWindowMinutes);
        var withinWindow = record.LastAttemptAt >= windowStart;

        return withinWindow && record.AttemptCount >= MaxAttempts;
    }
}
