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
                .AnyAsync(p => p.PharmacyId == pharmacyId, cancellationToken);

            if (!pharmacyExists)
                return Result.Failure<PharmacyDashboardDTO>(DashboardErrors.PharmacyNotFound);

            var branchIds = await context.PharmacyBranches
                .Where(b => b.PharmacyId == pharmacyId)
                .Select(b => b.BranchId)
                .ToListAsync(cancellationToken);

            var pharmacyOrderIds = context.OrderItems
                .Where(oi => oi.BranchId != null && branchIds.Contains(oi.BranchId.Value))
                .Select(oi => oi.OrderId)
                .Distinct();

            var kpis = await BuildKpisAsync(branchIds, pharmacyOrderIds, lowStockThreshold, cancellationToken);

            var lowStockAlert = new LowStockAlertDTO
            {
                LowStockCount = kpis.LowStockMedicinesCount,
                Threshold = lowStockThreshold,
                RestockNeeded = kpis.LowStockMedicinesCount > 0
            };

            var salesTrend = await BuildSalesTrendAsync(pharmacyOrderIds, cancellationToken);

            var recentOrders = await BuildRecentOrdersAsync(
                branchIds, pharmacyOrderIds, recentOrdersCount, cancellationToken);

            var dashboard = new PharmacyDashboardDTO
            {
                Kpis = kpis,
                LowStockAlert = lowStockAlert,
                SalesTrend = salesTrend,
                RecentOrders = recentOrders
            };

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
        CancellationToken cancellationToken = default) {
        if (id == Guid.Empty)
            return Result.Failure<PharmacyDashboardDTO>(DashboardErrors.BranchContextMissing);

        try {
            var pharmacyExists = await context.Pharmacies
                .AnyAsync(p => p.PharmacyId == pharmacyId, cancellationToken);

            if (!pharmacyExists)
                return Result.Failure<PharmacyDashboardDTO>(DashboardErrors.PharmacyNotFound);

            var branchExists = await context.PharmacyBranches
                .AnyAsync(b => b.BranchId == id && b.PharmacyId == pharmacyId, cancellationToken);

            if (!branchExists)
                return Result.Failure<PharmacyDashboardDTO>(DashboardErrors.BranchNotFound);

            var branchId = await context.PharmacyBranches
                .Where(b => b.PharmacyId == pharmacyId && b.BranchId == id)
                .Select(b => b.BranchId)
                .ToListAsync(cancellationToken);

            var pharmacyOrderIds = context.OrderItems
                .Where(oi => oi.BranchId != null && branchId.Contains(oi.BranchId.Value))
                .Select(oi => oi.OrderId)
                .Distinct();

            var kpis = await BuildKpisAsync(branchId, pharmacyOrderIds, lowStockThreshold, cancellationToken);

            var lowStockAlert = new LowStockAlertDTO {
                LowStockCount = kpis.LowStockMedicinesCount,
                Threshold = lowStockThreshold,
                RestockNeeded = kpis.LowStockMedicinesCount > 0
            };

            var salesTrend = await BuildSalesTrendAsync(pharmacyOrderIds, cancellationToken);

            var recentOrders = await BuildRecentOrdersAsync(
                branchId, pharmacyOrderIds, recentOrdersCount, cancellationToken);

            var dashboard = new PharmacyDashboardDTO {
                Kpis = kpis,
                LowStockAlert = lowStockAlert,
                SalesTrend = salesTrend,
                RecentOrders = recentOrders
            };

            return Result.Success(dashboard);
        }
        catch (Exception ex) {
            logger.LogError(ex, "Failed to build pharmacy dashboard for PharmacyId {PharmacyId}", pharmacyId);
            return Result.Failure<PharmacyDashboardDTO>(DashboardErrors.BranchRetrievalFailed);
        }
    }

    private async Task<PharmacyKpiDTO> BuildKpisAsync(
        List<Guid> branchIds,
        IQueryable<Guid> pharmacyOrderIds,
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
            .Where(i => branchIds.Contains(i.BranchId)
                        && (i.StockQuantity - i.ReservedQuantity) <= lowStockThreshold)
            .CountAsync(cancellationToken);

        var scopedOrders = context.Orders.Where(o => pharmacyOrderIds.Contains(o.OrderId));

        var todaysOrdersCount = await scopedOrders
            .CountAsync(o => o.CreatedAt >= today && o.CreatedAt < tomorrow, cancellationToken);

        var yesterdaysOrdersCount = await scopedOrders
            .CountAsync(o => o.CreatedAt >= yesterday && o.CreatedAt < today, cancellationToken);

        var monthlyRevenue = await scopedOrders
            .Where(o => o.OrderStatus != OrderStatus.Cancelled
                        && o.CreatedAt >= monthStart && o.CreatedAt < nextMonthStart)
            .SumAsync(o => (decimal?)o.TotalAmount, cancellationToken) ?? 0m;

        var prevMonthRevenue = await scopedOrders
            .Where(o => o.OrderStatus != OrderStatus.Cancelled
                        && o.CreatedAt >= prevMonthStart && o.CreatedAt < monthStart)
            .SumAsync(o => (decimal?)o.TotalAmount, cancellationToken) ?? 0m;

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
        IQueryable<Guid> pharmacyOrderIds,
        CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var windowStart = today.AddDays(-(SalesTrendDays - 1));
        var windowEnd = today.AddDays(1);

        var grouped = await context.Orders
            .Where(o => pharmacyOrderIds.Contains(o.OrderId)
                        && o.OrderStatus == OrderStatus.Completed
                        && o.DeliveredAt != null
                        && o.DeliveredAt >= windowStart && o.DeliveredAt < windowEnd)
            .GroupBy(o => o.DeliveredAt.Value.Date)
            .Select(g => new { Day = g.Key, Total = g.Sum(o => o.TotalAmount) })
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
        IQueryable<Guid> pharmacyOrderIds,
        int recentOrdersCount,
        CancellationToken cancellationToken)
    {
        var raw = await context.Orders
            .Where(o => pharmacyOrderIds.Contains(o.OrderId))
            .OrderByDescending(o => o.CreatedAt)
            .Take(recentOrdersCount)
            .Select(o => new
            {
                o.OrderId,
                o.CreatedAt,
                o.TotalAmount,
                o.OrderStatus,
                PatientName = o.Patient.FullName,
                Medicines = o.Items
                    .Where(i => i.BranchId != null && branchIds.Contains(i.BranchId.Value))
                    .Select(i => i.Drug.ArabicName)
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        return raw.Select(o => new PharmacyRecentOrderDTO
        {
            OrderId = o.OrderId,
            OrderNumber = BuildOrderNumber(o.OrderId),
            PatientName = string.IsNullOrWhiteSpace(o.PatientName) ? "Unknown" : o.PatientName,
            OrderedMedicinesCount = o.Medicines.Count,
            Summary = BuildMedicinesSummary(o.Medicines),
            TotalAmount = o.TotalAmount,
            OrderDate = o.CreatedAt,
            OrderStatus = o.OrderStatus,
            OrderStatusLabel = MapStatusLabel(o.OrderStatus)
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

    private static string MapStatusLabel(OrderStatus status) => status switch
    {
        OrderStatus.Pending => "Pending",
        OrderStatus.Processing => "Preparing",
        OrderStatus.Shipped => "Out for Delivery",
        OrderStatus.Completed => "Delivered",
        OrderStatus.Cancelled => "Cancelled",
        _ => status.ToString()
    };
}
