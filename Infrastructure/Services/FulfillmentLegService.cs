namespace Infrastructure.Services;

public class FulfillmentLegService(
    AppDbContext context,
    ILogger<FulfillmentLegService> logger) : IFulfillmentLegService
{
    public async Task<Result<bool>> GenerateLegsAsync(Guid orderId)
    {
        var order = await context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order is null)
            return Result.Failure<bool>(OrderErrors.OrderNotFound);

        var assignedItems = order.Items
            .Where(i => i.BranchId != Guid.Empty && i.BranchId != null)
            .ToList();

        if (assignedItems.Count == 0)
        {
            order.OrderStatus = OrderStatus.Cancelled;
            await context.SaveChangesAsync();
            return Result.Success(false);
        }

        var distinctBranchIds = assignedItems
            .Select(i => i.BranchId)
            .Distinct()
            .ToList();

        var calculatedLegType = order.FulfillmentMode == FulfillmentMode.Delivery
            ? LegType.Delivery
            : LegType.Preparation;

        var readyByEstimateTime = DateTime.UtcNow.AddMinutes(30);

        foreach (var branchId in distinctBranchIds)
        {
            var leg = new OrderFulfillmentLeg
            {
                LegId = Guid.NewGuid(),
                OrderId = order.OrderId,
                BranchId = branchId!.Value,
                LegStatus = LegStatus.Assigned,
                LegType = calculatedLegType,
                ReadyByEstimate = readyByEstimateTime
            };

            context.OrderFulfillmentLegs.Add(leg);
        }

        await context.SaveChangesAsync();

        return Result.Success(true);
    }

    public async Task<Result> UpdateLegStatusAsync(
        Guid legId, LegStatus newStatus, List<Guid> pharmacistBranchIds, CancellationToken cancellationToken)
    {
        var leg = await context.OrderFulfillmentLegs
            .Include(l => l.Order)
            .FirstOrDefaultAsync(l => l.LegId == legId, cancellationToken);

        if (leg is null)
            return Result.Failure(new Error("Leg.NotFound", "Fulfillment leg not found.", 404));

        if (!pharmacistBranchIds.Contains(leg.BranchId))
        {
            logger.LogWarning("Pharmacist attempted to update leg {LegId} outside their branch scope.", legId);
            return Result.Failure(new Error("Leg.Forbidden", "Branch mismatch.", 403));
        }

        var validationResult = ValidateStateTransition(leg.LegStatus, newStatus, leg.LegType);
        if (validationResult.IsFailure)
            return validationResult;

        return await ApplyStatusUpdateAsync(leg, newStatus, cancellationToken);
    }

    public async Task<Result> UpdateLegStatusForAdminAsync(
        Guid legId, LegStatus newStatus, string? auditReason, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(auditReason))
            return Result.Failure(new Error("Leg.AuditReasonRequired",
                "Audit reason is required for System Admin overrides.", 400));

        var leg = await context.OrderFulfillmentLegs
            .Include(l => l.Order)
            .FirstOrDefaultAsync(l => l.LegId == legId, cancellationToken);

        if (leg is null)
            return Result.Failure(new Error("Leg.NotFound", "Fulfillment leg not found.", 404));

        logger.LogWarning(
            "SYSTEM ADMIN OVERRIDE: Admin forced Leg {LegId} from {OldStatus} to {NewStatus}. Reason: {AuditReason}. Timestamp: {Timestamp}",
            legId, leg.LegStatus, newStatus, auditReason, DateTime.UtcNow);

        return await ApplyStatusUpdateAsync(leg, newStatus, cancellationToken);
    }

    private Result ValidateStateTransition(LegStatus oldStatus, LegStatus newStatus, LegType legType)
    {
        if (oldStatus == newStatus)
            return Result.Success();

        // Any state can be canceled technically, but Pharmacists usually just follow the happy path.
        if (newStatus == LegStatus.Cancelled)
            return Result.Success();

        bool isValid = oldStatus switch
        {
            LegStatus.Assigned => newStatus == LegStatus.Preparing,
            LegStatus.Preparing => (legType == LegType.Preparation && newStatus == LegStatus.ReadyForPickup) ||
                                   (legType == LegType.Delivery && newStatus == LegStatus.PickedUpByCourier),
            LegStatus.ReadyForPickup => newStatus == LegStatus.Completed,
            LegStatus.PickedUpByCourier => newStatus == LegStatus.Completed,
            _ => false
        };

        if (!isValid)
            return Result.Failure(new Error("Leg.InvalidTransition",
                $"Cannot transition leg from {oldStatus} to {newStatus} for type {legType}.", 400));

        return Result.Success();
    }

    private async Task<Result> ApplyStatusUpdateAsync(
        OrderFulfillmentLeg leg, LegStatus newStatus, CancellationToken cancellationToken)
    {
        leg.LegStatus = newStatus;

        if (newStatus == LegStatus.Completed)
            leg.CompletedAt = DateTime.UtcNow;

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        if (newStatus == LegStatus.Completed)
        {
            var siblingLegs = await context.OrderFulfillmentLegs
                .Where(l => l.OrderId == leg.OrderId)
                .Select(l => l.LegStatus)
                .ToListAsync(cancellationToken);

            var allCompleted = siblingLegs.All(s => s == LegStatus.Completed);
            if (allCompleted)
            {
                leg.Order.OrderStatus = OrderStatus.Completed;
                await context.SaveChangesAsync(cancellationToken);
            }
        }

        await transaction.CommitAsync(cancellationToken);

        return Result.Success();
    }
}