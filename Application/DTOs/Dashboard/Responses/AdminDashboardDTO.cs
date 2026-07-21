namespace Application.DTOs.Dashboard.Responses;

/// <summary>
/// Top-level response DTO for the System Administrator dashboard.
/// Aggregates platform-level statistics, order analytics, recent orders, and top pharmacies.
/// </summary>
public class AdminDashboardDTO
{
    /// <summary>
    /// Platform-wide KPI counters (patients, pharmacies, orders, medicines).
    /// </summary>
    public AdminPlatformStatsDTO PlatformStats { get; set; } = null!;

    /// <summary>
    /// Order analytics: 30-day daily counts and status distribution.
    /// </summary>
    public AdminOrderAnalyticsDTO OrderAnalytics { get; set; } = null!;

    /// <summary>
    /// The 10 most recently placed orders across the platform.
    /// </summary>
    public ICollection<AdminRecentOrderDTO> RecentOrders { get; set; }
        = new List<AdminRecentOrderDTO>();

    /// <summary>
    /// Top 5 pharmacies by number of fulfilled (completed) orders.
    /// </summary>
    public ICollection<AdminTopPharmacyDTO> TopPharmacies { get; set; }
        = new List<AdminTopPharmacyDTO>();
}

/// <summary>
/// Platform-wide KPI counters shown in the four stat cards.
/// </summary>
public class AdminPlatformStatsDTO
{
    /// <summary>Total number of registered patients.</summary>
    public int TotalPatients { get; set; }

    /// <summary>Total number of verified partner pharmacies.</summary>
    public int TotalPharmacies { get; set; }

    /// <summary>Total number of orders ever placed on the platform.</summary>
    public int TotalOrders { get; set; }

    /// <summary>Total number of distinct medicines in the drug catalog.</summary>
    public int TotalMedicines { get; set; }
}

/// <summary>
/// Order analytics data: daily order volume for the last 30 days and status distribution.
/// </summary>
public class AdminOrderAnalyticsDTO
{
    /// <summary>Daily order counts for the last 30 days (oldest first).</summary>
    public ICollection<AdminDailyOrderCountDTO> DailyOrdersLast30Days { get; set; }
        = new List<AdminDailyOrderCountDTO>();

    /// <summary>Number of orders currently in Pending status.</summary>
    public int PendingOrders { get; set; }

    /// <summary>Number of orders currently in Processing status.</summary>
    public int ProcessingOrders { get; set; }

    /// <summary>Number of orders currently in Shipped status.</summary>
    public int ShippedOrders { get; set; }

    /// <summary>Number of orders that have reached Completed status.</summary>
    public int CompletedOrders { get; set; }

    /// <summary>Number of orders that have been Cancelled.</summary>
    public int CancelledOrders { get; set; }
}

/// <summary>
/// Order count for a single calendar day.
/// </summary>
public class AdminDailyOrderCountDTO
{
    /// <summary>The calendar date (UTC).</summary>
    public DateOnly Date { get; set; }

    /// <summary>Number of orders created on this date.</summary>
    public int Count { get; set; }
}

/// <summary>
/// Summary of a single order shown in the Recent Orders table.
/// </summary>
public class AdminRecentOrderDTO
{
    /// <summary>Unique identifier of the order.</summary>
    public Guid OrderId { get; set; }

    /// <summary>Short human-readable order reference (first 8 chars of ID, upper-cased).</summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>Full name of the patient who placed the order.</summary>
    public string PatientName { get; set; } = string.Empty;

    /// <summary>Total monetary amount of the order.</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Current status of the order.</summary>
    public OrderStatus Status { get; set; }

    /// <summary>UTC timestamp when the order was created.</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Summary of a top-performing pharmacy for the Top Pharmacies section.
/// </summary>
public class AdminTopPharmacyDTO
{
    /// <summary>Unique identifier of the pharmacy.</summary>
    public Guid PharmacyId { get; set; }

    /// <summary>Legal/display name of the pharmacy.</summary>
    public string PharmacyName { get; set; } = string.Empty;

    /// <summary>
    /// Average star rating (0–5). Computed as a placeholder from completion rate;
    /// will be replaced when a dedicated rating column is added.
    /// </summary>
    public decimal Rating { get; set; }

    /// <summary>Primary branch address (first branch: city + address line).</summary>
    public string PrimaryAddress { get; set; } = string.Empty;

    /// <summary>Total number of completed orders fulfilled by this pharmacy.</summary>
    public int TotalOrders { get; set; }
}
