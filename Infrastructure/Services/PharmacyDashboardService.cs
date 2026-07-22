namespace Infrastructure.Services;

public class PharmacyDashboardService(AppDbContext context, ILogger<PharmacyDashboardService> logger)
    : IPharmacyDashboardService
{
    private const int SalesTrendDays = 7;

    public async Task<Result<PharmacyDashboardDTO>> GetPharmacyDashboardAsync(
        Guid pharmacyId,
        int lowStockThreshold,
        int recentOrdersCount,
        CancellationToken cancellationToken = default)
    {
        if (pharmacyId == Guid.Empty)
            return Result.Failure<PharmacyDashboardDTO>(DashboardErrors.PharmacyContextMissing);

        try
        {
            var pharmacyExists = await context.Pharmacies
                .AnyAsync(p => p.PharmacyId == pharmacyId 
                                && p.VerificationStatus == VerificationStatus.Verified,
                                cancellationToken);

            if (!pharmacyExists)
                return Result.Failure<PharmacyDashboardDTO>(DashboardErrors.PharmacyNotFound);

            var branches = await context.PharmacyBranches
                .Where(b => b.PharmacyId == pharmacyId)
                .Select(b => new BranchesDTO
                {
                    BranchId = b.BranchId,
                    BranchName = b.BranchName
                })
                .ToListAsync(cancellationToken);

            var branchIds = branches
                .Select(b => b.BranchId)
                .ToList();

            var dashboard = await BuildDashboardAsync(
                branches, branchIds, lowStockThreshold, recentOrdersCount, cancellationToken);

            return Result.Success(dashboard);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to build pharmacy dashboard for PharmacyId {PharmacyId}", pharmacyId);
            return Result.Failure<PharmacyDashboardDTO>(DashboardErrors.PharmacyRetrievalFailed);
        }
    }

    public async Task<Result<PharmacyDashboardDTO>> GetBranchDashboardAsync(
        Guid id,
        Guid pharmacyId,
        int lowStockThreshold,
        int recentOrdersCount,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            return Result.Failure<PharmacyDashboardDTO>(DashboardErrors.BranchContextMissing);

        try
        {
            var pharmacyExists = await context.Pharmacies
                .AnyAsync(p => p.PharmacyId == pharmacyId
                                && p.VerificationStatus == VerificationStatus.Verified,
                                cancellationToken);

            if (!pharmacyExists)
                return Result.Failure<PharmacyDashboardDTO>(DashboardErrors.PharmacyNotFound);

            var branchExists = await context.PharmacyBranches
                .AnyAsync(b => b.BranchId == id && b.PharmacyId == pharmacyId, cancellationToken);

            if (!branchExists)
                return Result.Failure<PharmacyDashboardDTO>(DashboardErrors.BranchNotFound);

            var branches = await context.PharmacyBranches
                .Where(b => b.PharmacyId == pharmacyId)
                .Select(b => new BranchesDTO
                {
                    BranchId = b.BranchId,
                    BranchName = b.BranchName
                })
                .ToListAsync(cancellationToken);

            var branchIds = new List<Guid> { id };

            var dashboard = await BuildDashboardAsync(
                branches, branchIds, lowStockThreshold, recentOrdersCount, cancellationToken);

            return Result.Success(dashboard);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to build branch dashboard for BranchId {BranchId}", id);
            return Result.Failure<PharmacyDashboardDTO>(DashboardErrors.BranchRetrievalFailed);
        }
    }

    private async Task<PharmacyDashboardDTO> BuildDashboardAsync(
        List<BranchesDTO> branches,
        List<Guid> branchIds,
        int lowStockThreshold,
        int recentOrdersCount,
        CancellationToken cancellationToken)
    {
        var kpis = await BuildKpisAsync(branchIds, lowStockThreshold, cancellationToken);

        var lowStockAlert = new LowStockAlertDTO
        {
            LowStockCount = kpis.LowStockMedicinesCount,
            Threshold = lowStockThreshold,
            RestockNeeded = kpis.LowStockMedicinesCount > 0
        };

        var salesTrend = await BuildSalesTrendAsync(branchIds, cancellationToken);

        var recentOrders = await BuildRecentOrdersAsync(branchIds, recentOrdersCount, cancellationToken);

        return new PharmacyDashboardDTO
        {
            Branches = branches,
            Kpis = kpis,
            LowStockAlert = lowStockAlert,
            SalesTrend = salesTrend,
            RecentOrders = recentOrders
        };
    }

    private async Task<PharmacyKpiDTO> BuildKpisAsync(
        List<Guid> branchIds,
        int lowStockThreshold,
        CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        var yesterday = today.AddDays(-1);

        var monthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var nextMonthStart = monthStart.AddMonths(1);
        var prevMonthStart = monthStart.AddMonths(-1);

        var totalMedicines = await context.PharmacyInventories
            .Where(i => branchIds.Contains(i.BranchId) && i.Drug.IsActive)
            .Select(i => i.DrugId)
            .Distinct()
            .CountAsync(cancellationToken);

        var lowStockCount = await context.PharmacyInventories
            .Where(i => branchIds.Contains(i.BranchId) && i.StockQuantity <= lowStockThreshold)
            .CountAsync(cancellationToken);

        var scopedLegs = context.OrderFulfillmentLegs
            .Where(l => branchIds.Contains(l.BranchId));

        var todaysOrdersCount = await scopedLegs
            .CountAsync(l => l.Order.CreatedAt >= today && l.Order.CreatedAt < tomorrow, cancellationToken);

        var yesterdaysOrdersCount = await scopedLegs
            .CountAsync(l => l.Order.CreatedAt >= yesterday && l.Order.CreatedAt < today, cancellationToken);

        var completedLegs = scopedLegs.Where(l => l.LegStatus == LegStatus.Delivered && l.CompletedAt != null);

        var monthlyRevenue = await completedLegs
            .Where(l => l.CompletedAt >= monthStart && l.CompletedAt < nextMonthStart)
            .SumAsync(l => (decimal?)l.Order.TotalAmount, cancellationToken) ?? 0m;

        var prevMonthRevenue = await completedLegs
            .Where(l => l.CompletedAt >= prevMonthStart && l.CompletedAt < monthStart)
            .SumAsync(l => (decimal?)l.Order.TotalAmount, cancellationToken) ?? 0m;

        return new PharmacyKpiDTO
        {
            TotalMedicines = totalMedicines,
            LowStockMedicinesCount = lowStockCount,
            TodaysOrdersCount = todaysOrdersCount,
            TodaysOrdersChangePercent = CalculatePercentChange(yesterdaysOrdersCount, todaysOrdersCount),
            MonthlyRevenue = monthlyRevenue,
            MonthlyRevenueChangePercent = CalculatePercentChange(prevMonthRevenue, monthlyRevenue)
        };
    }

    private async Task<List<DailySalesDTO>> BuildSalesTrendAsync(
        List<Guid> branchIds,
        CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var windowStart = today.AddDays(-(SalesTrendDays - 1));
        var windowEnd = today.AddDays(1);

        var grouped = await context.OrderFulfillmentLegs
            .Where(l => branchIds.Contains(l.BranchId)
                        && l.LegStatus == LegStatus.Delivered
                        && l.CompletedAt != null
                        && l.CompletedAt >= windowStart
                        && l.CompletedAt < windowEnd)
            .GroupBy(l => l.CompletedAt!.Value.Date)
            .Select(g => new { Day = g.Key, Total = g.Sum(l => l.Order.TotalAmount) })
            .ToListAsync(cancellationToken);

        var totalsByDay = grouped.ToDictionary(x => x.Day, x => x.Total);

        var trend = new List<DailySalesDTO>(SalesTrendDays);
        for (var i = 0; i < SalesTrendDays; i++)
        {
            var day = windowStart.AddDays(i);
            trend.Add(new DailySalesDTO
            {
                Date = day.ToString("yyyy-MM-dd"),
                SalesAmount = totalsByDay.TryGetValue(day, out var total) ? total : 0m
            });
        }

        return trend;
    }

    private async Task<List<PharmacyRecentOrderDTO>> BuildRecentOrdersAsync(
        List<Guid> branchIds,
        int recentOrdersCount,
        CancellationToken cancellationToken)
    {
        var raw = await context.OrderFulfillmentLegs
            .Where(l => branchIds.Contains(l.BranchId))
            .OrderByDescending(l => l.Order.CreatedAt)
            .ThenByDescending(l => l.CompletedAt)
            .Take(recentOrdersCount)
            .Select(l => new
            {
                l.LegId,
                l.OrderId,
                l.LegStatus,
                OrderCreatedAt = l.Order.CreatedAt,
                OrderTotal = l.Order.TotalAmount,
                PatientName = l.Order.Patient.FullName,
                Medicines = l.Order.Items
                    .Select(i => i.Drug.BrandName)
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        return raw.Select(l => new PharmacyRecentOrderDTO
        {
            LegId = l.LegId,
            OrderId = l.OrderId,
            OrderNumber = BuildOrderNumber(l.OrderId),
            PatientName = string.IsNullOrWhiteSpace(l.PatientName) ? "Unknown" : l.PatientName,
            OrderedMedicinesCount = l.Medicines.Count,
            Summary = BuildMedicinesSummary(l.Medicines),
            TotalAmount = l.OrderTotal,
            OrderDate = l.OrderCreatedAt,
            LegStatus = l.LegStatus,
            LegStatusLabel = MapLegStatusLabel(l.LegStatus)
        }).ToList();
    }

    private static decimal? CalculatePercentChange(decimal previous, decimal current)
    {
        if (previous == 0m) return null;
        return Math.Round((current - previous) / previous * 100m, 2);
    }

    private static string BuildOrderNumber(Guid orderId) =>
        $"ORD-{orderId.ToString("N")[..8].ToUpperInvariant()}";

    private static string BuildMedicinesSummary(IReadOnlyList<string> medicines)
    {
        var names = medicines
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .ToList();

        if (names.Count == 0) return "No medicines";

        const int previewCount = 2;
        var preview = string.Join(", ", names.Take(previewCount));
        var remaining = names.Count - previewCount;

        return remaining > 0 ? $"{preview} +{remaining} more" : preview;
    }

    private static string MapLegStatusLabel(LegStatus status) => status switch
    {
        LegStatus.Assigned => "Assigned",
        LegStatus.Preparing => "Preparing",
        LegStatus.ReadyForPickup => "Ready for Pickup",
        LegStatus.OutForDelivery => "Out for Delivery",
        LegStatus.Delivered => "Delivered",
        LegStatus.Cancelled => "Cancelled",
        _ => status.ToString()
    };
}
