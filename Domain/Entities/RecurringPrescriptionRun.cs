namespace Domain.Entities;

public enum RecurringRunStatus
{
    PendingConfirmation = 1,
    Confirmed = 2,
    Skipped = 3,
    Failed = 4,
    Completed = 5
}

public class RecurringPrescriptionRun
{
    public Guid Id { get; set; }
    public Guid RecurringPrescriptionId { get; set; }
    public Guid? OrderId { get; set; }

    public RecurringRunStatus Status { get; set; } = RecurringRunStatus.PendingConfirmation;
    public DateTime ScheduledAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? FailureReason { get; set; }

    // Confirmation token for email link
    public string? ConfirmationToken { get; set; }
    public DateTime? ConfirmationDeadline { get; set; }

    public RecurringPrescription RecurringPrescription { get; set; } = null!;
}