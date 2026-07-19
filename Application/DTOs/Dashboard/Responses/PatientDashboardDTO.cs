namespace Application.DTOs.Dashboard.Responses;

/// <summary>
/// Complete patient dashboard response containing statistics, current order, and recent orders.
/// </summary>
public class PatientDashboardDTO
{
    /// <summary>
    /// Dashboard statistics including total orders, pending reviews, saved addresses, and reward points.
    /// </summary>
    public DashboardStatisticsDTO Statistics { get; set; } = null!;

    /// <summary>
    /// Information about the current/most recent active order. Null if no active order exists.
    /// </summary>
    public CurrentOrderInfoDTO? CurrentOrder { get; set; }

    /// <summary>
    /// List of recent orders (excluding the current one) for quick access. Empty if no recent orders.
    /// </summary>
    public ICollection<RecentOrderSummaryDTO> RecentOrders { get; set; } 
        = new HashSet<RecentOrderSummaryDTO>();

    /// <summary>
    /// Indicates whether there are more orders beyond the recent orders list.
    /// </summary>
    public bool HasMoreOrders { get; set; }
}
