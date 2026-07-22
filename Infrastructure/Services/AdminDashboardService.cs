namespace Infrastructure.Services;

/// <summary>
/// Service implementation that aggregates platform-wide data for the System Administrator dashboard.
/// Queries patients, pharmacies, medicines, orders, and computes 30-day analytics.
/// </summary>
public class AdminDashboardService(
    AppDbContext context,
    ILogger<AdminDashboardService> logger) : IAdminDashboardService
{
    private const int AnalyticsDays = 30;

    public async Task<Result<AdminDashboardDTO>> GetAdminDashboardAsync(
        int recentOrdersCount = 10,
        int topPharmaciesCount = 5,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var platformStats = await GetPlatformStatsAsync(cancellationToken);
            var orderAnalytics = await GetOrderAnalyticsAsync(cancellationToken);
            var recentOrders = await GetRecentOrdersAsync(recentOrdersCount, cancellationToken);
            var topPharmacies = await GetTopPharmaciesAsync(topPharmaciesCount, cancellationToken);

            var dashboard = new AdminDashboardDTO
            {
                PlatformStats = platformStats,
                OrderAnalytics = orderAnalytics,
                RecentOrders = recentOrders,
                TopPharmacies = topPharmacies
            };

            return Result.Success(dashboard);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to build admin dashboard");
            return Result.Failure<AdminDashboardDTO>(
                new Error("AdminDashboard.Error", $"Failed to retrieve admin dashboard: {ex.Message}", 500));
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Platform Stats
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>Counts platform-wide entities for the four KPI stat cards.</summary>
    private async Task<AdminPlatformStatsDTO> GetPlatformStatsAsync(CancellationToken ct)
    {
        var totalPatients = await context.Patients.CountAsync(ct);
        var totalPharmacies = await context.Pharmacies
            .CountAsync(p => p.VerificationStatus == VerificationStatus.Verified, ct);
        var totalOrders = await context.Orders.CountAsync(ct);
        var totalMedicines = await context.Drugs.CountAsync(ct);

        return new AdminPlatformStatsDTO
        {
            TotalPatients = totalPatients,
            TotalPharmacies = totalPharmacies,
            TotalOrders = totalOrders,
            TotalMedicines = totalMedicines
        };
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Order Analytics
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds 30-day daily order counts and order status distribution.
    /// </summary>
    private async Task<AdminOrderAnalyticsDTO> GetOrderAnalyticsAsync(CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow.Date.AddDays(-AnalyticsDays + 1);

        // Daily order counts – group by date in application memory to avoid
        // EF Core translation issues with DateOnly on older providers.
        var ordersLast30Days = await context.Orders
            .Where(o => o.CreatedAt >= cutoff)
            .Select(o => o.CreatedAt.Date)
            .ToListAsync(ct);

        // Build a full 30-day series (filling in zeros for days with no orders)
        var dailyCounts = Enumerable
            .Range(0, AnalyticsDays)
            .Select(offset => cutoff.AddDays(offset).Date)
            .GroupJoin(
                ordersLast30Days.GroupBy(d => d).Select(g => new { Date = g.Key, Count = g.Count() }),
                day => day,
                grp => grp.Date,
                (day, grp) => new AdminDailyOrderCountDTO
                {
                    Date = DateOnly.FromDateTime(day),
                    Count = grp.FirstOrDefault()?.Count ?? 0
                })
            .ToList();

        // Status distribution across ALL orders (not just last 30 days)
        var statusCounts = await context.Orders
            .GroupBy(o => o.OrderStatus)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        int GetCount(OrderStatus status) =>
            statusCounts.FirstOrDefault(s => s.Status == status)?.Count ?? 0;

        return new AdminOrderAnalyticsDTO
        {
            DailyOrdersLast30Days = dailyCounts,
            PendingOrders = GetCount(OrderStatus.Pending),
            ProcessingOrders = GetCount(OrderStatus.Processing),
            ShippedOrders = GetCount(OrderStatus.Shipped),
            CompletedOrders = GetCount(OrderStatus.Completed),
            CancelledOrders = GetCount(OrderStatus.Cancelled)
        };
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Recent Orders
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the most recently placed orders with patient names joined from Identity.
    /// </summary>
    private async Task<ICollection<AdminRecentOrderDTO>> GetRecentOrdersAsync(
        int count, CancellationToken ct)
    {
        // Fetch raw data from DB (no range-index operators in expression trees)
        var rawOrders = await context.Orders
            .OrderByDescending(o => o.CreatedAt)
            .Take(count)
            .Join(
                context.Users,
                o => o.PatientUserId,
                u => u.Id,
                (o, u) => new
                {
                    o.OrderId,
                    o.TotalAmount,
                    o.OrderStatus,
                    o.CreatedAt,
                    u.FullName
                })
            .ToListAsync(ct);

        // Project in application memory where range operators are allowed
        return rawOrders.Select(o => new AdminRecentOrderDTO
        {
            OrderId = o.OrderId,
            OrderNumber = "ORD-" + o.OrderId.ToString().Substring(0, 8).ToUpper(),
            PatientName = o.FullName,
            TotalAmount = o.TotalAmount,
            Status = o.OrderStatus,
            CreatedAt = o.CreatedAt
        }).ToList();
    }


    // ──────────────────────────────────────────────────────────────────────────
    // Top Pharmacies
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the top pharmacies ranked by number of completed orders fulfilled via their branches.
    /// Rating is approximated as (completedOrders / totalOrders * 5) when totalOrders > 0.
    /// </summary>
    private async Task<ICollection<AdminTopPharmacyDTO>> GetTopPharmaciesAsync(
        int count, CancellationToken ct)
    {
        // Count completed fulfillment legs per pharmacy branch, then aggregate by pharmacy
        var topPharmacies = await context.Pharmacies
            .Where(p => p.VerificationStatus == VerificationStatus.Verified)
            .Select(p => new
            {
                p.PharmacyId,
                p.LegalName,
                PrimaryBranch = p.Branches.OrderBy(b => b.BranchId).FirstOrDefault(),
                CompletedLegs = p.Branches
                    .SelectMany(b => b.FulfillmentLegs)
                    .Count(l => l.LegStatus == LegStatus.Delivered),
                TotalLegs = p.Branches
                    .SelectMany(b => b.FulfillmentLegs)
                    .Count()
            })
            .OrderByDescending(p => p.CompletedLegs)
            .Take(count)
            .ToListAsync(ct);

        return topPharmacies.Select(p =>
        {
            // Approximate rating: completion rate scaled to 5 stars (min 3.0 if any orders exist)
            decimal rating = 0m;
            if (p.TotalLegs > 0)
            {
                var completionRate = (decimal)p.CompletedLegs / p.TotalLegs;
                rating = Math.Round(3.0m + completionRate * 2.0m, 1);
            }

            var address = p.PrimaryBranch is null
                ? "—"
                : $"{p.PrimaryBranch.Governorate}، {p.PrimaryBranch.City}";

            return new AdminTopPharmacyDTO
            {
                PharmacyId = p.PharmacyId,
                PharmacyName = p.LegalName,
                Rating = rating,
                PrimaryAddress = address,
                TotalOrders = p.CompletedLegs
            };
        }).ToList();
    }
}
