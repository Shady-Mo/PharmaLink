namespace Application.DTOs.PharmacyBranch.Request;

/// <summary>Represents one day's schedule in a PUT request.</summary>
public class BranchScheduleDayRequest
{
    /// <summary>Day of week (0 = Sunday … 6 = Saturday).</summary>
    public DayOfWeek Day { get; set; }

    /// <summary>Opening time in "HH:mm" (24-hour). Ignored when IsClosed = true.</summary>
    public string? OpenTime { get; set; }

    /// <summary>Closing time in "HH:mm" (24-hour). Ignored when IsClosed = true.</summary>
    public string? CloseTime { get; set; }

    /// <summary>True when the branch does not operate on this day.</summary>
    public bool IsClosed { get; set; }
}

/// <summary>Full-week schedule update sent by PharmacyAdmin.</summary>
public class UpdateBranchScheduleRequest
{
    /// <summary>Must contain exactly 7 entries, one per day of the week.</summary>
    public List<BranchScheduleDayRequest> Schedule { get; set; } = new();
}
