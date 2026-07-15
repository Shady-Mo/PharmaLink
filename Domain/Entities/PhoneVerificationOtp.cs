namespace Domain.Entities;

public class PhoneVerificationOtp
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string CodeHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public int AttemptCount { get; set; }

    public DateTime? LastAttemptAt { get; set; }

    public AppUser User { get; set; } = null!;
}
