using Application.Services.OrderSplitting.Models;
using System.Diagnostics;

namespace Infrastructure.Services;

public class OrderSplittingService(
    AppDbContext context,
    IGeoLookupService geoLookupService,
    IInventoryService inventoryService,
    ILegGenerationService legGenerationService,
    IOrderSplittingAlgorithm
    splittingAlgorithm,
    ILogger<OrderSplittingService> logger) : IOrderSplittingService
{
    public async Task<Result> SplitOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        logger.LogInformation("[OrderId={OrderId}] Starting split process.", orderId);

        return await ExecuteWithRetryAsync(orderId, async (order, transaction, ct) =>
        {
            var result = await ExecuteSplitInternalAsync(order, transaction, ct);
            return result;
        }, cancellationToken);
    }

    public async Task<Result> ResplitOrderAsync(Guid orderId, Guid adminUserId, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        logger.LogInformation("Admin {AdminId} initiated re-split for Order {OrderId}", adminUserId, orderId);

        return await ExecuteWithRetryAsync(orderId, async (order, transaction, ct) =>
        {
            var preSplitStatus = string.Join(", ", order.Items.GroupBy(i => i.ItemStatus).Select(g => $"{g.Key}: {g.Count()}"));
            logger.LogInformation("[OrderId={OrderId}] Pre-split state: {State}", orderId, preSplitStatus);

            var awardedItems = order.Items.Where(i => i.ItemStatus == ItemStatus.Awarded && i.BranchId.HasValue).ToList();
            var releases = awardedItems
                .GroupBy(i => new { i.BranchId, i.DrugId })
                .Select(g => (g.Key.BranchId!.Value, g.Key.DrugId, g.Sum(x => x.QuantityNeeded)))
                .ToList();

            if (releases.Any())
            {
                var releaseResult = await inventoryService.ReleaseReservationBatchAsync(releases, ct);
                if (releaseResult.IsFailure)
                    logger.LogWarning("[OrderId={OrderId}] Failed to release batched reservations during re-split.", orderId);
            }

            context.OrderFulfillmentLegs.RemoveRange(order.FulfillmentLegs);
            logger.LogInformation("[OrderId={OrderId}] Deleted {Count} existing legs", orderId, order.FulfillmentLegs.Count);
            order.FulfillmentLegs.Clear();

            foreach (var item in order.Items.Where(i => i.ItemStatus != ItemStatus.Cancelled))
            {
                item.BranchId = null;
                item.ItemStatus = ItemStatus.Pending;
            }

            order.OrderStatus = OrderStatus.Pending;

            // Execute the split immediately after cleanup
            var splitResult = await ExecuteSplitInternalAsync(order, transaction, ct);

            sw.Stop();
            logger.LogInformation("Admin {AdminId} completed re-split for Order {OrderId} in {ElapsedMs}ms. Success: {IsSuccess}", adminUserId, orderId, sw.ElapsedMilliseconds, splitResult.IsSuccess);

            return splitResult;
        }, cancellationToken);
    }

    private async Task<Result> ExecuteWithRetryAsync(Guid orderId, Func<Order, Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction, CancellationToken, Task<Result>> action, CancellationToken cancellationToken)
    {
        const int maxRetries = 3;
        var totalSw = Stopwatch.StartNew();

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var loadSw = Stopwatch.StartNew();
                var order = await LoadOrderAsync(orderId, cancellationToken);
                if (order is null)
                {
                    logger.LogWarning("[OrderId={OrderId}] Order not found.", orderId);
                    return Result.Failure(OrderSplittingErrors.OrderNotFound);
                }
                loadSw.Stop();
                logger.LogInformation("[OrderId={OrderId}] Order loaded in {ElapsedMs}ms. Status={Status}, Items={Count}", orderId, loadSw.ElapsedMilliseconds, order.OrderStatus, order.Items.Count);

                var validationResult = ValidateOrderForSplit(order);
                if (validationResult.IsFailure)
                {
                    logger.LogWarning("[OrderId={OrderId}] Order validation failed: {Error}. Marking as failed without modifying items.", orderId, validationResult.Error.Description);
                    return validationResult;
                }

                var result = await action(order, transaction, cancellationToken);

                if (result.IsSuccess)
                {
                    await transaction.CommitAsync(cancellationToken);
                    totalSw.Stop();
                    logger.LogInformation("[OrderId={OrderId}] Transaction committed successfully. Total execution time: {ElapsedMs}ms.", orderId, totalSw.ElapsedMilliseconds);
                }
                else
                {
                    await transaction.RollbackAsync(cancellationToken);
                }

                return result;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                logger.LogWarning(ex, "[OrderId={OrderId}] Concurrency conflict detected on attempt {Attempt}/{MaxRetries}.", orderId, attempt, maxRetries);

                if (attempt == maxRetries)
                {
                    logger.LogError("[OrderId={OrderId}] Max retries reached. Failing split.", orderId);
                    return Result.Failure(OrderSplittingErrors.TransactionFailed);
                }

                // Clear the change tracker so the next attempt loads fresh data and re-evaluates all business rules.
                context.ChangeTracker.Clear();
                // Brief delay before retry to allow concurrent operations to settle
                await Task.Delay(100 * attempt, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[OrderId={OrderId}] Split failed due to an unexpected error. Rolling back.", orderId);
                await transaction.RollbackAsync(cancellationToken);
                return Result.Failure(OrderSplittingErrors.TransactionFailed);
            }
        }

        return Result.Failure(OrderSplittingErrors.TransactionFailed);
    }

    private async Task<Result> ExecuteSplitInternalAsync(Order order, Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction, CancellationToken cancellationToken)
    {
        order.OrderStatus = OrderStatus.Processing;

        var pendingItems = order.Items.Where(i => i.ItemStatus == ItemStatus.Pending).ToList();
        var drugIds = pendingItems.Select(i => i.DrugId).ToHashSet();

        var geoSw = Stopwatch.StartNew();
        var nearbyBranches = await LoadNearbyBranchesAsync(order, cancellationToken);
        geoSw.Stop();
        logger.LogInformation("[OrderId={OrderId}] Geo lookup completed in {ElapsedMs}ms. Found {Count} eligible branches.", order.OrderId, geoSw.ElapsedMilliseconds, nearbyBranches.Count);

        if (nearbyBranches.Count == 0)
        {
            logger.LogWarning("[OrderId={OrderId}] No eligible branches. Keeping order as Processing/Pending and marking items Unavailable.", order.OrderId);
            foreach (var item in pendingItems) item.ItemStatus = ItemStatus.Unavailable;
            // Order stays in Processing, items Unavailable. Wait for manual intervention or re-split.
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        var branchIds = nearbyBranches.Select(b => b.BranchID).ToHashSet();

        var invSw = Stopwatch.StartNew();
        var inventorySnapshot = await LoadInventorySnapshotAsync(branchIds, drugIds, cancellationToken);
        invSw.Stop();
        logger.LogInformation("[OrderId={OrderId}] Inventory query completed in {ElapsedMs}ms. {Count} records found.", order.OrderId, invSw.ElapsedMilliseconds, inventorySnapshot.Count);

        var candidateBranches = BuildCandidateBranches(nearbyBranches, inventorySnapshot);

        var pendingItemModels = pendingItems.Select(i => new PendingItem(i.OrderItemId, i.DrugId, i.QuantityNeeded)).ToList();
        var splitContext = new SplittingContext(order.OrderId, order.FulfillmentMode, pendingItemModels, candidateBranches);

        var algoSw = Stopwatch.StartNew();
        var splitResult = splittingAlgorithm.Execute(splitContext);
        algoSw.Stop();
        logger.LogInformation("[OrderId={OrderId}] Algorithm {AlgorithmName} completed in {ElapsedMs}ms. Assigned={AssignedCount}, Unassigned={UnassignedCount}", order.OrderId, splittingAlgorithm.AlgorithmName, algoSw.ElapsedMilliseconds, splitResult.Assignments.Count, splitResult.UnassignedItemIds.Count);

        ApplySplitResultToItems(pendingItems, splitResult);

        var resSw = Stopwatch.StartNew();
        var reservationFailures = ReserveStockBatchedAsync(splitResult.Assignments, inventorySnapshot);
        resSw.Stop();
        logger.LogInformation("[OrderId={OrderId}] Reservation logic completed in {ElapsedMs}ms.", order.OrderId, resSw.ElapsedMilliseconds);

        if (reservationFailures.Any())
        {
            logger.LogWarning("[OrderId={OrderId}] Stock reservation failed for {Count} items. Marking as Unavailable.", order.OrderId, reservationFailures.Count);
            MarkReservationFailuresUnavailable(order.Items, reservationFailures);
        }

        var assignedBranchIds = order.Items.Where(i => i.ItemStatus == ItemStatus.Awarded).Select(i => i.BranchId!.Value).ToHashSet();
        if (!assignedBranchIds.Any())
        {
            logger.LogWarning("[OrderId={OrderId}] No items could be fulfilled. Order remains Processing, items Unavailable.", order.OrderId);
            await context.SaveChangesAsync(cancellationToken); // Save empty split status
            return Result.Success();
        }

        var legSw = Stopwatch.StartNew();
        var newLegs = legGenerationService.GenerateLegs(order, assignedBranchIds).Value;
        context.OrderFulfillmentLegs.AddRange(newLegs);
        legSw.Stop();
        logger.LogInformation("[OrderId={OrderId}] Fulfillment legs generated in {ElapsedMs}ms.", order.OrderId, legSw.ElapsedMilliseconds);

        await context.SaveChangesAsync(cancellationToken); // Save assignments, status, and generated legs once

        return Result.Success();
    }

    private async Task<Order?> LoadOrderAsync(Guid orderId, CancellationToken cancellationToken)
    {
        return await context.Orders
            .Include(o => o.Items)
            .Include(o => o.DeliveryAddress)
            .Include(o => o.FulfillmentLegs) // Needed for Resplit cleanup
            .AsSplitQuery()
            .FirstOrDefaultAsync(o => o.OrderId == orderId, cancellationToken);
    }

    private Result ValidateOrderForSplit(Order order)
    {
        if (order.DeliveryAddress?.GeoLocation is null)
            return Result.Failure(OrderSplittingErrors.NoGeoLocation);

        if (order.OrderStatus != OrderStatus.Pending && order.OrderStatus != OrderStatus.Processing)
            return Result.Failure(OrderSplittingErrors.NotEligibleForSplit);

        return Result.Success();
    }

    private async Task<List<NearbyBranchResult>> LoadNearbyBranchesAsync(Order order, CancellationToken cancellationToken)
    {
        var branches = await geoLookupService.FindNearbyBranchesAsync(order.DeliveryAddress.GeoLocation!, 5.0, cancellationToken);
        return branches.Where(b => order.FulfillmentMode == FulfillmentMode.Delivery ? b.SupportsDelivery : b.SupportsPickup).ToList();
    }

    private async Task<List<PharmacyInventory>> LoadInventorySnapshotAsync(HashSet<Guid> branchIds, HashSet<Guid> drugIds, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await context.PharmacyInventories
            .Where(i => branchIds.Contains(i.BranchId) && drugIds.Contains(i.DrugId) && i.ExpiryDate > today)
            .ToListAsync(cancellationToken);
    }

    private List<CandidateBranch> BuildCandidateBranches(List<NearbyBranchResult> nearbyBranches, List<PharmacyInventory> inventorySnapshot)
    {
        var inventoryLookup = inventorySnapshot.ToLookup(i => i.BranchId);

        return nearbyBranches.Select(b => new CandidateBranch(
            b.BranchID,
            b.BranchName,
            b.DistanceKm,
            b.SupportsDelivery,
            b.SupportsPickup,
            inventoryLookup[b.BranchID].ToDictionary(i => i.DrugId, i => i.StockQuantity - i.ReservedQuantity)
        )).ToList();
    }

    private void ApplySplitResultToItems(List<OrderItem> pendingItems, SplittingResult splitResult)
    {
        var assignmentDict = splitResult.Assignments.ToDictionary(a => a.OrderItemId);

        foreach (var item in pendingItems)
        {
            if (assignmentDict.TryGetValue(item.OrderItemId, out var assignment))
            {
                item.BranchId = assignment.BranchId;
                item.ItemStatus = ItemStatus.Awarded;

                logger.LogInformation(
                    "[OrderId={OrderId}] DrugId={DrugId} Assigned to Branch={BranchId}. Reason: [Strategy={Strategy}, Coverage={Coverage}, Distance={Distance}km, RemainingStock={Stock}]",
                    item.OrderId, item.DrugId, assignment.BranchId, assignment.Decision.Strategy, assignment.Decision.Coverage, assignment.Decision.DistanceKm, assignment.Decision.RemainingStock);
            }
            else
            {
                item.BranchId = null;
                item.ItemStatus = ItemStatus.Unavailable;
            }
        }
    }

    private List<Guid> ReserveStockBatchedAsync(IReadOnlyList<ItemAssignment> assignments, List<PharmacyInventory> inventorySnapshot)
    {
        var failedItemIds = new List<Guid>();

        var reservations = assignments
            .GroupBy(a => new { a.BranchId, a.DrugId })
            .Select(g => (g.Key.BranchId, g.Key.DrugId, Quantity: g.Sum(x => x.QuantityNeeded)))
            .ToList();

        if (!reservations.Any()) return failedItemIds;

        var result = inventoryService.ReserveStockBatch(inventorySnapshot, reservations);
        if (result.IsFailure)
        {
            // The Orchestrator does not catch DbUpdateConcurrencyException here because
            // InventoryService no longer calls SaveChangesAsync.
            // If validation inside ReserveStockBatchAsync fails (e.g. InsufficientStock),
            // it means the snapshot was stale. We mark them as failed.
            failedItemIds.AddRange(assignments.Select(a => a.OrderItemId));
        }

        return failedItemIds;
    }

    private void MarkReservationFailuresUnavailable(ICollection<OrderItem> items, List<Guid> failedItemIds)
    {
        var failedSet = failedItemIds.ToHashSet();
        foreach (var item in items.Where(i => failedSet.Contains(i.OrderItemId)))
        {
            item.ItemStatus = ItemStatus.Unavailable;
            item.BranchId = null;
        }
    }
}