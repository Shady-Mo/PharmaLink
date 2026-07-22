using Application.DTOs.Dashboard.Responses;

namespace Application.Services.Dashboard;

/// <summary>
/// Service for aggregating the System Administrator dashboard.
/// Provides platform-wide statistics, order analytics for the last 30 days,
/// recent order history, and top-performing pharmacies.
/// </summary>
public interface IAdminDashboardService
{
    /// <summary>
    /// Builds the complete administrator dashboard aggregating platform-level data.
    /// </summary>
    /// <param name="recentOrdersCount">
    /// Number of most-recent orders to include in the dashboard (default: 10, max: 50).
    /// </param>
    /// <param name="topPharmaciesCount">
    /// Number of top pharmacies to include (default: 5).
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A <see cref="Result{AdminDashboardDTO}"/> containing the full dashboard,
    /// or a failure result if a database error occurs.
    /// </returns>
    Task<Result<AdminDashboardDTO>> GetAdminDashboardAsync(
        int recentOrdersCount = 10,
        int topPharmaciesCount = 5,
        CancellationToken cancellationToken = default);
}
