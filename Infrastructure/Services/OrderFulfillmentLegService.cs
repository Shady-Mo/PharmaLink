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

        var role = GetUserRole(user);

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
        leg.CompletedAt = request.Status == LegStatus.Delivered
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

        if (leg.Order.FulfillmentLegs.All(l => l.LegStatus == LegStatus.Delivered))
            leg.Order.OrderStatus = OrderStatus.Completed;

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
         user.Claims
             .Where(c => c.Type.Equals("BranchId", StringComparison.OrdinalIgnoreCase) || c.Type == JwtClaimTypes.BranchId)
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
            (LegStatus.Preparing, LegStatus.OutForDelivery) => true,
            (LegStatus.ReadyForPickup, LegStatus.Delivered) => true,
            (LegStatus.OutForDelivery, LegStatus.Delivered) => true,
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

    private static string? GetUserRole(ClaimsPrincipal user) =>
        user.Claims.FirstOrDefault(c =>
            c.Type.Equals("RoleName", StringComparison.OrdinalIgnoreCase) ||
            c.Type.Equals("role", StringComparison.OrdinalIgnoreCase) ||
            c.Type == JwtClaimTypes.RoleName)?.Value;
    private static List<Guid> GetUserBranchIds(ClaimsPrincipal user) =>
        user.Claims
            .Where(c => c.Type.Equals("BranchId", StringComparison.OrdinalIgnoreCase) || c.Type == "branch_id")
            .Select(c => c.Value)
            .Where(value => Guid.TryParse(value, out _))
            .Select(Guid.Parse)
            .ToList();

    public async Task<Result<PaginatedList<BranchOrderRowDto>>> GetBranchOrdersAsync(
        GetBranchOrdersRequest request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var branchIds = GetUserBranchIds(user);

        if (!branchIds.Any())
        {
            return Result.Failure<PaginatedList<BranchOrderRowDto>>(OrderFulfillmentLegErrors.Forbidden);
        }

        var query = dbContext.OrderFulfillmentLegs.AsNoTracking()
            .Where(leg => branchIds.Contains(leg.BranchId));

        if (request.Status.HasValue)
        {
            query = query.Where(leg => leg.LegStatus == request.Status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var projectedQuery = await query
            .OrderByDescending(leg => leg.ReadyByEstimate)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(leg => new
            {
                leg.OrderId,
                PatientName = leg.Order.Patient != null ? leg.Order.Patient.FullName : "غير معروف",
                TotalAmount = leg.Order.TotalAmount,
                leg.LegStatus,
                leg.ReadyByEstimate,
                DrugsList = dbContext.OrderItems
                    .Where(oi => oi.OrderId == leg.OrderId && oi.BranchId == leg.BranchId)
                    .Select(oi => (oi.Drug.BrandName ?? oi.Drug.GenericName) + " × " + oi.QuantityNeeded)
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        var dtos = projectedQuery.Select(item => new BranchOrderRowDto
        {
            OrderId = item.OrderId,
            OrderNumber = $"ORD-{item.OrderId.ToString().Substring(0, 8).ToUpper()}",
            PatientName = item.PatientName,
            TotalAmount = item.TotalAmount,
            Date = item.ReadyByEstimate,
            DrugsSummary = string.Join("، ", item.DrugsList),
            Status = (LegStatus)item.LegStatus
        }).ToList();

        var paginatedList = new PaginatedList<BranchOrderRowDto>(dtos, request.PageNumber, totalCount, request.PageSize);

        return Result.Success(paginatedList);
    }
}
