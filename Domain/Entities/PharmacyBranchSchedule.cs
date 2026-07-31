namespace Domain.Entities;

/// <summary>
/// Represents the working schedule for a single day of the week for a pharmacy branch.
/// </summary>
public class PharmacyBranchSchedule
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }

    /// <summary>0 = Sunday … 6 = Saturday (matches System.DayOfWeek).</summary>
    public DayOfWeek Day { get; set; }

    /// <summary>Opening time. Null when IsClosed = true.</summary>
    public TimeOnly? OpenTime { get; set; }

    /// <summary>Closing time. Null when IsClosed = true.</summary>
    public TimeOnly? CloseTime { get; set; }

    /// <summary>True when the branch is closed the whole day.</summary>
    public bool IsClosed { get; set; }

    public PharmacyBranch Branch { get; set; } = null!;
}
