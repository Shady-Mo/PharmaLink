using System.Security.Claims;
using Application.DTOs.OrderFulfillmentLeg.Requests;
using Application.DTOs.OrderFulfillmentLeg.Responses;

namespace Infrastructure.Services;

public class OrderFulfillmentLegService(AppDbContext dbContext) : IOrderFulfillmentLegService
{
    public async Task<Result<OrderFulfillmentLegDto>> GetByIdAsync(
        Guid legId,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var leg = await dbContext.OrderFulfillmentLegs
            .Include(l => l.Order)
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.LegId == legId, cancellationToken);

        if (leg is null)
            return Result.Failure<OrderFulfillmentLegDto>(OrderFulfillmentLegErrors.NotFound);

        return CanReadLeg(leg, user)
            ? Result.Success(ToDto(leg))
            : Result.Failure<OrderFulfillmentLegDto>(OrderFulfillmentLegErrors.Forbidden);
    }

    public async Task<Result<OrderFulfillmentLegDto>> UpdateStatusAsync(
        Guid legId,
        UpdateLegStatusRequest request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var leg = await dbContext.OrderFulfillmentLegs
            .Include(l => l.Order)
            .ThenInclude(o => o.FulfillmentLegs)
            .FirstOrDefaultAsync(l => l.LegId == legId, cancellationToken);

        if (leg is null)
            return Result.Failure<OrderFulfillmentLegDto>(OrderFulfillmentLegErrors.NotFound);

        var role = user.FindFirstValue(JwtClaimTypes.RoleName);

        if (role == AppRoles.Patient)
            return Result.Failure<OrderFulfillmentLegDto>(OrderFulfillmentLegErrors.Forbidden);

        if (role == AppRoles.Pharmacist)
        {
            if (!UserHasBranchScope(user, leg.BranchId))
                return Result.Failure<OrderFulfillmentLegDto>(OrderFulfillmentLegErrors.Forbidden);

            if (!IsAllowedPharmacistTransition(leg.LegStatus, request.Status))
                return Result.Failure<OrderFulfillmentLegDto>(OrderFulfillmentLegErrors.InvalidTransition);
        }
        else if (role == AppRoles.Admin)
        {
            if (string.IsNullOrWhiteSpace(request.Reason))
                return Result.Failure<OrderFulfillmentLegDto>(OrderFulfillmentLegErrors.OverrideReasonRequired);
        }
        else
        {
            return Result.Failure<OrderFulfillmentLegDto>(OrderFulfillmentLegErrors.Forbidden);
        }

        var oldStatus = leg.LegStatus;
        leg.LegStatus = request.Status;
        leg.CompletedAt = request.Status == LegStatus.Completed
            ? leg.CompletedAt ?? DateTime.UtcNow
            : null;

        if (role == AppRoles.Admin)
        {
            var userId = GetCurrentUserId(user);
            if (userId is null)
                return Result.Failure<OrderFulfillmentLegDto>(OrderFulfillmentLegErrors.InvalidUserContext);

            dbContext.OrderFulfillmentLegStatusAudits.Add(new OrderFulfillmentLegStatusAudit
            {
                AuditId = Guid.NewGuid(),
                LegId = leg.LegId,
                ChangedByUserId = userId.Value,
                OldStatus = oldStatus,
                NewStatus = request.Status,
                Reason = request.Reason!.Trim(),
                ChangedAtUtc = DateTime.UtcNow
            });
        }

        if (leg.Order.FulfillmentLegs.All(l => l.LegStatus == LegStatus.Completed))
        {
            leg.Order.OrderStatus = OrderStatus.Completed;
            leg.Order.DeliveredAt = leg.Order.DeliveredAt ?? DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(ToDto(leg));
    }

    private static bool CanReadLeg(OrderFulfillmentLeg leg, ClaimsPrincipal user)
    {
        var role = user.FindFirstValue(JwtClaimTypes.RoleName);

        return role switch
        {
            AppRoles.Admin => true,
            AppRoles.Pharmacist => UserHasBranchScope(user, leg.BranchId),
            AppRoles.Patient => GetCurrentUserId(user) == leg.Order.PatientUserId,
            _ => false
        };
    }

    private static bool UserHasBranchScope(ClaimsPrincipal user, Guid branchId) =>
        user.FindAll(JwtClaimTypes.BranchId)
            .Select(c => c.Value)
            .Any(value => Guid.TryParse(value, out var claimBranchId) && claimBranchId == branchId);

    private static Guid? GetCurrentUserId(ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(JwtClaimTypes.UserId), out var userId)
            ? userId
            : null;

    private static bool IsAllowedPharmacistTransition(LegStatus oldStatus, LegStatus newStatus) =>
        (oldStatus, newStatus) switch
        {
            (LegStatus.Assigned, LegStatus.Preparing) => true,
            (LegStatus.Preparing, LegStatus.ReadyForPickup) => true,
            (LegStatus.Preparing, LegStatus.PickedUpByCourier) => true,
            (LegStatus.ReadyForPickup, LegStatus.Completed) => true,
            (LegStatus.PickedUpByCourier, LegStatus.Completed) => true,
            _ => false
        };

    private static OrderFulfillmentLegDto ToDto(OrderFulfillmentLeg leg) => new()
    {
        LegId = leg.LegId,
        OrderId = leg.OrderId,
        BranchId = leg.BranchId,
        LegType = leg.LegType,
        LegStatus = leg.LegStatus,
        ReadyByEstimate = leg.ReadyByEstimate,
        CompletedAt = leg.CompletedAt
    };
}
