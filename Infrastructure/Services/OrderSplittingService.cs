namespace Infrastructure.Services;

public class OrderSplittingService(
    AppDbContext context,
    IGeoLookupService geoLookupService,
    IInventoryService inventoryService,
    IFulfillmentLegService fulfillmentLegService,
    ILogger<OrderSplittingService> logger) : IOrderSplittingService
{
    public async Task<Result> SplitOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await context.Orders
            .Include(o => o.Items)
            .Include(o => o.DeliveryAddress)
            .FirstOrDefaultAsync(o => o.OrderId == orderId, cancellationToken);

        if (order is null)
            return Result.Failure(OrderErrors.OrderNotFound);

        if (order.DeliveryAddress.GeoLocation is null)
        {
            foreach (var item in order.Items.Where(i => i.ItemStatus == ItemStatus.Pending))
            {
                item.ItemStatus = ItemStatus.Unavailable;
            }

            await context.SaveChangesAsync(cancellationToken);
            return Result.Failure(OrderErrors.OrderDeliveryAddressHasNoLocation);
        }

        order.OrderStatus = OrderStatus.Processing;
        await context.SaveChangesAsync(cancellationToken);

        var nearbyBranches =
            await geoLookupService.FindNearbyBranchesAsync(order.DeliveryAddress.GeoLocation, 5.0, cancellationToken);

        if (nearbyBranches.Count == 0)
        {
            foreach (var item in order.Items.Where(i => i.ItemStatus == ItemStatus.Pending))
            {
                item.ItemStatus = ItemStatus.Unavailable;
            }

            await context.SaveChangesAsync(cancellationToken);
            await fulfillmentLegService.GenerateLegsAsync(orderId);
            return Result.Success();
        }

        var nearbyBranchIds = nearbyBranches.Select(b => b.BranchID).ToHashSet();
        var pendingItems = order.Items.Where(i => i.ItemStatus == ItemStatus.Pending).ToList();
        var drugIds = pendingItems.Select(i => i.DrugId).ToHashSet();

        var inventorySnapshot = await context.PharmacyInventories
            .AsNoTracking()
            .Where(i => nearbyBranchIds.Contains(i.BranchId) && drugIds.Contains(i.DrugId))
            .Select(i => new { i.BranchId, i.DrugId, Available = i.StockQuantity - i.ReservedQuantity })
            .ToListAsync(cancellationToken);

        var branchScores = nearbyBranches
            .Select(b => new
            {
                Branch = b,
                Coverage = pendingItems.Count(item => inventorySnapshot.Any(inv =>
                    inv.BranchId == b.BranchID && inv.DrugId == item.DrugId && inv.Available >= item.QuantityNeeded))
            })
            .OrderByDescending(x => x.Coverage)
            .ThenBy(x => x.Branch.DistanceKm)
            .ToList();

        var selectedBranchIds = new HashSet<Guid>();

        var ledger = inventorySnapshot.ToDictionary(
            x => (x.BranchId, x.DrugId),
            x => x.Available
        );

        foreach (var item in pendingItems)
        {
            bool assigned = false;

            foreach (var selectedBranchId in selectedBranchIds)
            {
                if (ledger.TryGetValue((selectedBranchId, item.DrugId), out int available) &&
                    available >= item.QuantityNeeded)
                {
                    item.BranchId = selectedBranchId;
                    item.ItemStatus = ItemStatus.Awarded;
                    ledger[(selectedBranchId, item.DrugId)] -= item.QuantityNeeded;
                    assigned = true;
                    break;
                }
            }

            if (!assigned)
            {
                foreach (var branchScore in branchScores)
                {
                    var branchId = branchScore.Branch.BranchID;
                    if (ledger.TryGetValue((branchId, item.DrugId), out int available) &&
                        available >= item.QuantityNeeded)
                    {
                        item.BranchId = branchId;
                        item.ItemStatus = ItemStatus.Awarded;
                        ledger[(branchId, item.DrugId)] -= item.QuantityNeeded;
                        selectedBranchIds.Add(branchId);
                        assigned = true;
                        break;
                    }
                }
            }

            if (!assigned)
            {
                item.ItemStatus = ItemStatus.Unavailable;
            }
        }

        foreach (var item in pendingItems.Where(i => i.ItemStatus == ItemStatus.Awarded && i.BranchId.HasValue))
        {
            var reserveResult = await inventoryService.ReserveStockAsync(item.BranchId.Value, item.DrugId,
                item.QuantityNeeded, cancellationToken);

            if (!reserveResult.IsFailure) continue;

            logger.LogWarning(
                "Failed to reserve stock for Order {OrderId}, Item {OrderItemId} at Branch {BranchId}. Marking as Unavailable. Error: {Error}",
                order.OrderId, item.OrderItemId, item.BranchId, reserveResult.Error.Description);
            item.ItemStatus = ItemStatus.Unavailable;
            item.BranchId = null;
        }

        await context.SaveChangesAsync(cancellationToken);
        await fulfillmentLegService.GenerateLegsAsync(orderId);

        return Result.Success();
    }

    public async Task<Result> ResplitOrderAsync(Guid orderId, Guid adminUserId,
        CancellationToken cancellationToken = default)
    {
        var order = await context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderId == orderId, cancellationToken);

        if (order is null)
            return Result.Failure(OrderErrors.OrderNotFound);

        if (order.OrderStatus != OrderStatus.Pending && order.OrderStatus != OrderStatus.Processing)
            return Result.Failure(OrderErrors.OrderNotEligibleForResplit);

        var preSplitStatus = order.Items.GroupBy(i => i.ItemStatus)
            .Select(g => $"{g.Key}: {g.Count()}")
            .ToList();

        logger.LogInformation(
            "Admin {AdminId} initiated re-split for Order {OrderId} at {Time}. Previous state: {State}",
            adminUserId, orderId, DateTime.UtcNow, string.Join(", ", preSplitStatus));

        foreach (var item in order.Items.Where(i => i.ItemStatus == ItemStatus.Awarded && i.BranchId.HasValue))
        {
            var releaseResult = await inventoryService.ReleaseReservationAsync(item.BranchId.Value, item.DrugId,
                item.QuantityNeeded, cancellationToken);
            if (releaseResult.IsFailure)
            {
                logger.LogWarning(
                    "Failed to release reservation during re-split for Order {OrderId}, Item {OrderItemId} at Branch {BranchId}. Error: {Error}",
                    order.OrderId, item.OrderItemId, item.BranchId, releaseResult.Error.Description);
            }
        }

        foreach (var item in order.Items.Where(i => i.ItemStatus != ItemStatus.Cancelled))
        {
            item.BranchId = null;
            item.ItemStatus = ItemStatus.Pending;
        }

        await context.SaveChangesAsync(cancellationToken);

        var splitResult = await SplitOrderAsync(orderId, cancellationToken);

        var postSplitStatus = order.Items.GroupBy(i => i.ItemStatus)
            .Select(g => $"{g.Key}: {g.Count()}")
            .ToList();

        logger.LogInformation("Admin {AdminId} completed re-split for Order {OrderId}. New state: {State}",
            adminUserId, orderId, string.Join(", ", postSplitStatus));

        return splitResult;
    }
}