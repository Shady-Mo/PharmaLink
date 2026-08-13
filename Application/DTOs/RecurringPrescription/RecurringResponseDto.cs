namespace Application.DTOs.RecurringPrescription;

public class RecurringResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public int IntervalDays { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public DateOnly NextRunDate { get; set; }
    public string FulfillmentMode { get; set; } = string.Empty;
    public Guid? PreferredBranchId { get; set; }
    public string? PreferredBranchName { get; set; }
    public bool RequireConfirmation { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? PrescriptionImageUrl { get; set; }
    public List<RecurringRunDto> RecentRuns { get; set; } = new();
}

public class RecurringRunDto
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ScheduledAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public Guid? OrderId { get; set; }
}
