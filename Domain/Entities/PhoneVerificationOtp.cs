namespace Domain.Entities;

/// <summary>
/// Stores the active OTP state for a user's phone verification request.
/// One active record per user at most — upserted on each new /request call.
/// </summary>
public class PhoneVerificationOtp
{
    public Guid Id { get; set; }

    /// <summary>FK to AspNetUsers.</summary>
    public Guid UserId { get; set; }

    /// <summary>BCrypt hash of the 6-digit code. The plain-text is never persisted.</summary>
    public string CodeHash { get; set; } = string.Empty;

    /// <summary>UTC expiry timestamp (issued time + 5 minutes).</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>Rolling count of verify attempts against this OTP record.</summary>
    public int AttemptCount { get; set; }

    /// <summary>UTC timestamp of the last attempt — anchors the 15-minute rate-limit window.</summary>
    public DateTime? LastAttemptAt { get; set; }

    // Navigation
    public AppUser User { get; set; } = null!;
}
