namespace Application.DTOs.PharmacyBranch.Response;

/// <summary>Single-day schedule entry returned to clients.</summary>
public class BranchScheduleDayDto
{
    /// <summary>Day of week (0 = Sunday … 6 = Saturday).</summary>
    public DayOfWeek Day { get; set; }

    /// <summary>Localised Arabic day name.</summary>
    public string DayNameAr { get; set; } = string.Empty;

    /// <summary>Opening time in "HH:mm" (24-hour). Null when IsClosed.</summary>
    public string? OpenTime { get; set; }

    /// <summary>Closing time in "HH:mm" (24-hour). Null when IsClosed.</summary>
    public string? CloseTime { get; set; }

    /// <summary>True when the branch does not operate on this day.</summary>
    public bool IsClosed { get; set; }

    /// <summary>True when this entry corresponds to today.</summary>
    public bool IsToday { get; set; }

    /// <summary>True when the branch is currently open (only meaningful when IsToday).</summary>
    public bool IsCurrentlyOpen { get; set; }
}

/// <summary>Full-week schedule response for a branch.</summary>
public class BranchScheduleResponseDto
{
    public Guid BranchId { get; set; }
    public List<BranchScheduleDayDto> Days { get; set; } = new();
}
