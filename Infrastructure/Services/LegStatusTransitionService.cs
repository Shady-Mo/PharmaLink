using Application.Services.FulfillmentLeg;
using Domain.Entities;
using Domain.Enums;
using Application.Common;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Application.Abstractions;

namespace Infrastructure.Services;

public class LegStatusTransitionService(
    AppDbContext context,
    ILogger<LegStatusTransitionService> logger,
    IWebPushNotificationService pushNotificationService) : ILegStatusTransitionService
{
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
                                   (legType == LegType.Delivery && newStatus == LegStatus.OutForDelivery),
            LegStatus.ReadyForPickup => newStatus == LegStatus.Delivered,
            LegStatus.OutForDelivery => newStatus == LegStatus.Delivered,
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

        if (newStatus == LegStatus.Delivered)
        {
            leg.CompletedAt = DateTime.UtcNow;
            
            bool anyIncomplete = await context.OrderFulfillmentLegs
                .AnyAsync(l => l.OrderId == leg.OrderId && l.LegId != leg.LegId && l.LegStatus != LegStatus.Delivered && l.LegStatus != LegStatus.Cancelled, cancellationToken);

            if (!anyIncomplete)
            {
                leg.Order.OrderStatus = OrderStatus.Completed;
                await pushNotificationService.SendNotificationAsync(leg.Order.PatientUserId, "تم التسليم بنجاح!", "تم توصيل طلبك بالكامل. شكراً لاستخدامك PharmaLink.", $"/patient/orders/{leg.OrderId}");
            }
            else
            {
                await pushNotificationService.SendNotificationAsync(leg.Order.PatientUserId, "تم توصيل جزء من الطلب", "تم تسليم جزء من طلبك بنجاح.", $"/patient/orders/{leg.OrderId}");
            }
        }
        else if (newStatus == LegStatus.OutForDelivery)
        {
            await pushNotificationService.SendNotificationAsync(leg.Order.PatientUserId, "طلبك في الطريق!", "الطيار في طريقه إليك لتوصيل الطلب.", $"/patient/orders/{leg.OrderId}");
        }
        else if (newStatus == LegStatus.ReadyForPickup)
        {
            await pushNotificationService.SendNotificationAsync(leg.Order.PatientUserId, "الطلب جاهز للاستلام", "طلبك جاهز الآن للاستلام من الصيدلية.", $"/patient/orders/{leg.OrderId}");
        }

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
