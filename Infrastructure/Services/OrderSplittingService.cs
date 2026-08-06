using Application.DTOs.OrderRouting;
using Application.Services.OrderRouting;
using Application.Services.OrderSplitting.Models;
using System.Diagnostics;

namespace Infrastructure.Services;

/// <summary>
/// Splits an order across pharmacy branches using ONLY the AI Order Fulfillment Optimization
/// Engine (<see cref="IOrderRoutingOrchestrator.OptimizeOrderFulfillmentAsync"/>) — the exact
/// same entry point the <c>OrderRoutingController</c> preview uses.
///
/// The engine owns everything decision-related: it queries live inventory, computes real-world
/// OSRM driving distances per branch, and decides the item→branch allocation (minimizing the
/// number of fulfilling branches first, distance second). This service no longer performs any
/// geo lookup, candidate-branch distance math, or deterministic fallback algorithm — it simply
/// translates the engine's plan into item assignments, reserves stock, and generates legs.
/// </summary>
public class OrderSplittingService(
    AppDbContext context,
    IInventoryService inventoryService,
    ILegGenerationService legGenerationService,
    IOrderRoutingOrchestrator orderRoutingOrchestrator,
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

        // Drug names give the fulfillment engine richer context for its reasoning; mapping the
        // resulting plan back to items is always done by DrugId, so a missing name is harmless.
        var nameByDrug = await context.Drugs
            .Where(d => drugIds.Contains(d.DrugId))
            .Select(d => new { d.DrugId, d.BrandName })
            .ToDictionaryAsync(d => d.DrugId, d => d.BrandName, cancellationToken);

        // ── DECISION BRAIN — AI Order Fulfillment Optimization Engine (identical to the
        //    OrderRoutingController preview flow). The engine internally queries live inventory,
        //    computes real-world OSRM driving distances per branch, and decides the optimal
        //    item→branch allocation. There is no deterministic fallback: if the engine cannot
        //    place items, they are simply marked Unavailable for manual intervention/re-split. ──
        var patientLocation = new GeoLocation(
            order.DeliveryAddress!.GeoLocation!.Y,
            order.DeliveryAddress!.GeoLocation!.X);

        var cartForRouting = pendingItems
            .Select(i => new CartItemDto
            {
                DrugId = i.DrugId,
                DrugName = nameByDrug.TryGetValue(i.DrugId, out var name) ? name : string.Empty,
                Quantity = i.QuantityNeeded
            })
            .ToList();

        var algoSw = Stopwatch.StartNew();
        OrderRoutingPlan plan;
        try
        {
            plan = await orderRoutingOrchestrator.OptimizeOrderFulfillmentAsync(
                order.PatientUserId, patientLocation, cartForRouting, order.FulfillmentMode, cancellationToken);

        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[OrderId={OrderId}] AI fulfillment engine failed. Marking pending items Unavailable.", order.OrderId);
            foreach (var item in pendingItems) item.ItemStatus = ItemStatus.Unavailable;
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        algoSw.Stop();

        var splitResult = MapPlanToSplittingResult(plan, pendingItems);
        logger.LogInformation(
            "[OrderId={OrderId}] AI-FulfillmentEngine ({Strategy}) completed in {ElapsedMs}ms. Legs={LegCount}, Assigned={AssignedCount}, Unassigned={UnassignedCount}, TotalDistanceKm={TotalKm:F2}",
            order.OrderId, plan.Strategy, algoSw.ElapsedMilliseconds, plan.FulfillmentLegCount, splitResult.Assignments.Count, splitResult.UnassignedItemIds.Count, plan.TotalDistanceKm);

        if (splitResult.Assignments.Count == 0)
        {
            logger.LogWarning("[OrderId={OrderId}] Engine produced no fulfillable assignments. Marking items Unavailable.", order.OrderId);
            foreach (var item in pendingItems) item.ItemStatus = ItemStatus.Unavailable;
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        ApplySplitResultToItems(pendingItems, splitResult);

        // Load the authoritative inventory rows for the branches/drugs the engine chose so we can
        // reserve stock (and re-validate against live data) inside the transaction.
        var assignedBranchIds = splitResult.Assignments.Select(a => a.BranchId).ToHashSet();
        var invSw = Stopwatch.StartNew();
        var inventorySnapshot = await LoadInventorySnapshotAsync(assignedBranchIds, drugIds, cancellationToken);
        invSw.Stop();
        logger.LogInformation("[OrderId={OrderId}] Inventory snapshot for reservation loaded in {ElapsedMs}ms. {Count} records.", order.OrderId, invSw.ElapsedMilliseconds, inventorySnapshot.Count);

        var resSw = Stopwatch.StartNew();
        var reservationFailures = ReserveStockBatchedAsync(splitResult.Assignments, inventorySnapshot);
        resSw.Stop();
        logger.LogInformation("[OrderId={OrderId}] Reservation logic completed in {ElapsedMs}ms.", order.OrderId, resSw.ElapsedMilliseconds);

        if (reservationFailures.Any())
        {
            logger.LogWarning("[OrderId={OrderId}] Stock reservation failed for {Count} items. Marking as Unavailable.", order.OrderId, reservationFailures.Count);
            MarkReservationFailuresUnavailable(order.Items, reservationFailures);
        }

        var fulfilledBranchIds = order.Items.Where(i => i.ItemStatus == ItemStatus.Awarded).Select(i => i.BranchId!.Value).ToHashSet();
        if (!fulfilledBranchIds.Any())
        {
            logger.LogWarning("[OrderId={OrderId}] No items could be fulfilled. Order remains Processing, items Unavailable.", order.OrderId);
            await context.SaveChangesAsync(cancellationToken); // Save empty split status
            return Result.Success();
        }

        // Carry the AI engine's OSRM driving distance per branch onto the persisted legs so the
        // order response returns the SAME distance as the order-routing preview (not straight-line).
        var distanceByBranchKm = plan.Legs
            .GroupBy(l => l.BranchId)
            .ToDictionary(g => g.Key, g => g.First().DistanceKm);

        var legSw = Stopwatch.StartNew();
        var newLegs = legGenerationService.GenerateLegs(order, fulfilledBranchIds, distanceByBranchKm).Value;
        context.OrderFulfillmentLegs.AddRange(newLegs!);

        legSw.Stop();
        logger.LogInformation("[OrderId={OrderId}] Fulfillment legs generated in {ElapsedMs}ms.", order.OrderId, legSw.ElapsedMilliseconds);

        await context.SaveChangesAsync(cancellationToken); // Save assignments, status, and generated legs once

        return Result.Success();
    }

    /// <summary>
    /// Translates the <see cref="OrderRoutingPlan"/> produced by the AI engine into per-item
    /// <see cref="ItemAssignment"/>s. The engine already validated availability against live
    /// inventory, so mapping is done directly by DrugId per leg; the subsequent stock-reservation
    /// step provides the final authoritative safety check. Any pending item the plan does not
    /// place is returned as unassigned so it is marked Unavailable downstream.
    /// </summary>
    private SplittingResult MapPlanToSplittingResult(OrderRoutingPlan plan, List<OrderItem> pendingItems)
    {
        // Queue the concrete pending order-items per drug so multiple lines of the same drug are
        // each given their own assignment record against the engine's per-branch line quantity.
        var pendingByDrug = pendingItems
            .GroupBy(i => i.DrugId)
            .ToDictionary(g => g.Key, g => new Queue<OrderItem>(g));

        var assignments = new List<ItemAssignment>();

        foreach (var leg in plan.Legs)
        {
            foreach (var line in leg.Items)
            {
                if (!pendingByDrug.TryGetValue(line.DrugId, out var queue))
                    continue;

                var remainingQty = line.Quantity;
                while (queue.Count > 0 && queue.Peek().QuantityNeeded <= remainingQty)
                {
                    var item = queue.Dequeue();
                    remainingQty -= item.QuantityNeeded;

                    assignments.Add(new ItemAssignment(
                        item.OrderItemId,
                        leg.BranchId,
                        item.DrugId,
                        item.QuantityNeeded,
                        new AssignmentDecision(
                            $"AI-FulfillmentEngine:{plan.Strategy}",
                            plan.Legs.Count,
                            leg.DistanceKm,
                            remainingQty)));
                }
            }
        }

        var assignedItemIds = assignments.Select(a => a.OrderItemId).ToHashSet();
        var unassigned = pendingItems
            .Where(i => !assignedItemIds.Contains(i.OrderItemId))
            .Select(i => i.OrderItemId)
            .ToList();

        return new SplittingResult(assignments, unassigned);
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

    private async Task<List<PharmacyInventory>> LoadInventorySnapshotAsync(HashSet<Guid> branchIds, HashSet<Guid> drugIds, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await context.PharmacyInventories
            .Where(i => branchIds.Contains(i.BranchId) && drugIds.Contains(i.DrugId) && i.ExpiryDate > today)
            .ToListAsync(cancellationToken);
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
