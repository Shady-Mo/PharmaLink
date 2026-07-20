using Application.DTOs.Dashboard.Responses;

namespace Application.Services.Dashboard;

/// <summary>
/// Service for aggregating the Pharmacy Owner dashboard: KPIs, low stock alerts,
/// 7-day sales trend, and recent orders. All data is strictly scoped to a single pharmacy.
/// </summary>
public interface IPharmacyDashboardService
{
    /// <summary>
    /// Builds the complete pharmacy owner dashboard for the given pharmacy.
    /// </summary>
    /// <param name="pharmacyId">The pharmacy the authenticated owner is scoped to (from JWT claims).</param>
    /// <param name="lowStockThreshold">Available-quantity threshold at/below which an item is low stock.</param>
    /// <param name="recentOrdersCount">Number of recent orders to include (default 10).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A <see cref="Result{PharmacyDashboardDTO}"/> containing the aggregated dashboard,
    /// or a failure when the pharmacy context is missing/invalid.
    /// </returns>
    Task<Result<PharmacyDashboardDTO>> GetPharmacyDashboardAsync(
        Guid pharmacyId,
        int lowStockThreshold,
        int recentOrdersCount,
        CancellationToken cancellationToken = default);

    Task<Result<PharmacyDashboardDTO>> GetBranchDashboardAsync(
        Guid id,
        Guid pharmacyId,
        int lowStockThreshold,
        int recentOrdersCount,
        CancellationToken cancellationToken = default);
}
