namespace Infrastructure.Services;

/// <summary>
/// Service implementation for retrieving patient dashboard information.
/// Aggregates statistics, current order status, and recent order history.
/// </summary>
public class DashboardService(AppDbContext context) : IDashboardService
{
    public async Task<Result<PatientDashboardDTO>> GetDashboardAsync(
        Guid patientUserId,
        int recentOrdersCount = 5,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Verify patient exists
            var patientExists = await context.Patients
                .AnyAsync(p => p.Id == patientUserId, cancellationToken);

            if (!patientExists)
                return Result.Failure<PatientDashboardDTO>(
                    new Error("Patient.NotFound", "Patient not found", 404));

            // Get dashboard statistics
            var statistics = await GetStatisticsAsync(patientUserId, cancellationToken);

            // Get current order (most recent active order)
            var currentOrder = await GetCurrentOrderAsync(patientUserId, cancellationToken);

            // Get recent orders (excluding current order)
            var recentOrders = await GetRecentOrdersAsync(
                patientUserId,
                recentOrdersCount,
                currentOrder?.OrderId,
                cancellationToken);

            var dashboard = new PatientDashboardDTO
            {
                Statistics = statistics,
                CurrentOrder = currentOrder,
                RecentOrders = recentOrders,
                HasMoreOrders = await HasMoreOrdersAsync(patientUserId, recentOrdersCount, cancellationToken)
            };

            return Result.Success(dashboard);
        }
        catch (Exception ex)
        {
            return Result.Failure<PatientDashboardDTO>(
                new Error("Dashboard.Error", $"Failed to retrieve dashboard: {ex.Message}", 500));
        }
    }

    /// <summary>
    /// Retrieves dashboard statistics for the patient.
    /// </summary>
    private async Task<DashboardStatisticsDTO> GetStatisticsAsync(
        Guid patientUserId,
        CancellationToken cancellationToken)
    {
        var totalOrders = await context.Orders
            .CountAsync(o => o.PatientUserId == patientUserId, cancellationToken);

        var pendingReviews = await context.PrescriptionReviews
            .CountAsync(pr => pr.PatientUserId == patientUserId
                           && pr.ReviewStatus == PrescriptionReviewStatus.PendingReview,
                cancellationToken);

        var savedAddresses = await context.Addresses
            .CountAsync(a => a.UserId == patientUserId, cancellationToken);

        // Reward points: placeholder implementation (can be enhanced with a loyalty system)
        // For now, returning 0. This can be linked to completed orders or a separate points table
        var rewardPoints = 0;

        return new DashboardStatisticsDTO
        {
            TotalOrders = totalOrders,
            PendingPrescriptionReviews = pendingReviews,
            SavedAddresses = savedAddresses,
            RewardPoints = rewardPoints
        };
    }

    /// <summary>
    /// Retrieves the current (most recent active) order with its fulfillment progress.
    /// </summary>
    private async Task<CurrentOrderInfoDTO?> GetCurrentOrderAsync(
        Guid patientUserId,
        CancellationToken cancellationToken)
    {
        // Get the most recent order (active or most recently created)
        var order = await context.Orders
            .Where(o => o.PatientUserId == patientUserId)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (order is null)
            return null;

        // Get fulfillment legs for the order
        var fulfillmentLegs = await context.OrderFulfillmentLegs
            .Where(leg => leg.OrderId == order.OrderId)
            .Include(leg => leg.Branch)
            .OrderBy(leg => leg.LegId)
            .ToListAsync(cancellationToken);

        var progressTimeline = fulfillmentLegs.Select(leg => new OrderProgressStepDTO
        {
            FulfillmentLegId = leg.LegId,
            LegType = leg.LegType,
            Status = leg.LegStatus,
            PharmacyName = leg.Branch?.BranchName,
            EstimatedCompletionTime = leg.ReadyByEstimate
        }).ToList();

        return new CurrentOrderInfoDTO
        {
            OrderId = order.OrderId,
            Status = order.OrderStatus,
            ProgressTimeline = progressTimeline
        };
    }

    /// <summary>
    /// Retrieves recent orders for the patient (excluding current order).
    /// </summary>
    private async Task<ICollection<RecentOrderSummaryDTO>> GetRecentOrdersAsync(
        Guid patientUserId,
        int count,
        Guid? currentOrderId,
        CancellationToken cancellationToken)
    {
        var recentOrders = await context.Orders
            .Where(o => o.PatientUserId == patientUserId
                     && (currentOrderId == null || o.OrderId != currentOrderId))
            .OrderByDescending(o => o.CreatedAt)
            .Take(count)
            .Include(o => o.Items)
                .ThenInclude(oi => oi.Drug)
            .ToListAsync(cancellationToken);

        var summaries = new List<RecentOrderSummaryDTO>();

        foreach (var order in recentOrders)
        {
            var medicines = order.Items.Select(item => new OrderedMedicineDTO
            {
                DrugId = item.DrugId,
                DrugName = item.Drug?.BrandName ?? "Unknown Medicine",
                Quantity = item.QuantityNeeded
            }).ToList();

            summaries.Add(new RecentOrderSummaryDTO
            {
                OrderId = order.OrderId,
                OrderNumber = order.OrderId.ToString().Substring(0, 8).ToUpper(),
                OrderDate = order.CreatedAt,
                Medicines = medicines,
                TotalAmount = order.TotalAmount,
                Status = order.OrderStatus
            });
        }

        return summaries;
    }

    /// <summary>
    /// Checks if there are more orders beyond the retrieved recent orders.
    /// </summary>
    private async Task<bool> HasMoreOrdersAsync(
        Guid patientUserId,
        int recentOrdersCount,
        CancellationToken cancellationToken)
    {
        var totalOrders = await context.Orders
            .CountAsync(o => o.PatientUserId == patientUserId, cancellationToken);

        // Adding 1 to account for the current order if it exists
        var currentOrderCount = 1;
        return totalOrders > (recentOrdersCount + currentOrderCount);
    }
}
