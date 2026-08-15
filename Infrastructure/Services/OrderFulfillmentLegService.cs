using Application.DTOs.DeliveryDriver;
using Application.DTOs.OrderFulfillmentLeg.Requests;
using Application.DTOs.OrderFulfillmentLeg.Responses;

namespace Infrastructure.Services;

public class OrderFulfillmentLegService(
    AppDbContext dbContext,
    IDeliveryDriverService driverService,
    IDeliveryNotificationService notificationService,
    IWebPushNotificationService pushNotificationService) : IOrderFulfillmentLegService
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
            .Include(o => o.Branch)
            .Include(l => l.Order).ThenInclude(a => a.DeliveryAddress)
            .Include(l => l.Order).ThenInclude(o => o.FulfillmentLegs)
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
            Reason = string.IsNullOrWhiteSpace(request.Reason) ? request.Status.ToString() : request.Reason.Trim(),
            ChangedAtUtc = DateTime.UtcNow
        });

        if (leg.Order.FulfillmentLegs.All(l =>
                l.LegStatus == LegStatus.Delivered || l.LegStatus == LegStatus.Cancelled))
            leg.Order.OrderStatus = OrderStatus.Completed;


        if (request.Status == LegStatus.OutForDelivery && leg.LegType == LegType.Delivery)
        {
            var address = leg.Order.DeliveryAddress;
            var fullAddress =
                $"{address.BuildingNumber} عمارة, دور {address.FloorNumber}, {address.AddressLine}, {address.City}";

            double distanceKm = 0;
            if (leg.Branch.GeoLocation != null && address.GeoLocation != null)
            {
                distanceKm = CalculateDistanceKm(
                    leg.Branch.GeoLocation.Y, leg.Branch.GeoLocation.X,
                    address.GeoLocation.Y, address.GeoLocation.X
                );

                distanceKm = Math.Round(distanceKm, 2);
            }

            decimal baseFee = 15.0m;
            decimal pricePerExtraKm = 5.0m;
            decimal calculatedFee = baseFee;

            if (distanceKm > 2.0)
            {
                calculatedFee += (decimal)(distanceKm - 2.0) * pricePerExtraKm;
            }

            calculatedFee = Math.Round(calculatedFee, 0);

            var deliveryJob = new DeliveryJob
            {
                JobId = Guid.NewGuid(),
                LegId = leg.LegId,
                Status = DeliveryJobStatus.Pending,
                DeliveryFee = calculatedFee
            };

            dbContext.DeliveryJobs.Add(deliveryJob);
            await dbContext.SaveChangesAsync(cancellationToken);

            var nearbyDriversResult = await driverService.GetNearbyAvailableDriversAsync(leg.BranchId);

            if (nearbyDriversResult.IsSuccess && nearbyDriversResult.Value.Any())
            {
                var jobDetails = new DeliveryJobNotificationDto
                {
                    JobId = deliveryJob.JobId,
                    PharmacyName = leg.Branch.BranchName,
                    FullAddress = fullAddress,
                    DeliveryFee = deliveryJob.DeliveryFee,
                    DistanceKm = distanceKm,
                    Latitude = address.GeoLocation.Y,
                    Longitude = address.GeoLocation.X,
                    PharmacyLatitude = leg.Branch.GeoLocation.Y,
                    PharmacyLongitude = leg.Branch.GeoLocation.X
                };

                await notificationService.BroadcastNewDeliveryJobAsync(nearbyDriversResult.Value, jobDetails);
            }
        }

        var title = "";
        var message = "";

        switch (request.Status)
        {
            case LegStatus.Preparing:
                title = "جاري تحضير طلبك ⏳";
                message = $"تم البدء في تجهيز جزء من طلبك من صيدلية {leg.Branch.BranchName}.";
                break;
            case LegStatus.ReadyForPickup:
                title = "طلبك جاهز للاستلام 🛍️";
                message = $"جزء من طلبك جاهز الآن للاستلام من صيدلية {leg.Branch.BranchName}.";
                break;
            case LegStatus.OutForDelivery:
                title = "طلبك في الطريق إليك 🚚";
                message = $"شحنة من طلبك خرجت للتوصيل من صيدلية {leg.Branch.BranchName}.";
                break;
            case LegStatus.Cancelled:
                title = "تم إلغاء شحنة من طلبك ❌";
                message = $"تم إلغاء شحنة من طلبك من صيدلية {leg.Branch.BranchName} لعدم التوفر.";
                break;
        }

        if (!string.IsNullOrEmpty(title))
        {
            await pushNotificationService.SendNotificationAsync(
                leg.Order.PatientUserId,
                title,
                message,
                "/patient/orders"
            );
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
        user.Claims
            .Where(c => c.Type.Equals("BranchId", StringComparison.OrdinalIgnoreCase) ||
                        c.Type == JwtClaimTypes.BranchId)
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
                PatientName = leg.Order.Patient != null ? leg.Order.Patient.FullName : null,
                TotalAmount = leg.Order.TotalAmount,
                leg.LegStatus,
                leg.ReadyByEstimate,
                Items = dbContext.OrderItems
                    .Where(oi => oi.OrderId == leg.OrderId && oi.BranchId == leg.BranchId)
                    .Select(oi => new { oi.Drug.BrandName, oi.Drug.ArabicName, oi.QuantityNeeded })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        var dtos = projectedQuery.Select(item => new BranchOrderRowDto
        {
            OrderId = item.OrderId,
            OrderNumber = $"ORD-{item.OrderId.ToString().Substring(0, 8).ToUpper()}",
            PatientName = item.PatientName ?? "غير معروف",
            TotalAmount = item.TotalAmount,
            Date = item.ReadyByEstimate,
            DrugsSummary = string.Join("، ",
                item.Items.Select(oi =>
                    (!string.IsNullOrWhiteSpace(oi.BrandName) ? oi.BrandName : oi.ArabicName) + " × " +
                    oi.QuantityNeeded)),
            Status = (LegStatus)item.LegStatus
        }).ToList();

        var paginatedList =
            new PaginatedList<BranchOrderRowDto>(dtos, request.PageNumber, totalCount, request.PageSize);

        return Result.Success(paginatedList);
    }

    public async Task<Result<PharmacistOrderDetailsDto>> GetPharmacistOrderDetailsAsync(
        Guid orderId,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var branchIds = GetUserBranchIds(user);

        if (!branchIds.Any())
        {
            return Result.Failure<PharmacistOrderDetailsDto>(OrderFulfillmentLegErrors.Forbidden);
        }

        var order = await dbContext.Orders
            .Include(o => o.Patient)
            .Include(o => o.PrescriptionReview)
            .Include(o => o.Items)
            .ThenInclude(i => i.Drug)
            .Include(o => o.FulfillmentLegs)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.OrderId == orderId, cancellationToken);

        if (order is null)
        {
            return Result.Failure<PharmacistOrderDetailsDto>(OrderFulfillmentLegErrors.NotFound);
        }

        var assignedLeg = order.FulfillmentLegs.FirstOrDefault(l => branchIds.Contains(l.BranchId));
        if (assignedLeg is null)
        {
            return Result.Failure<PharmacistOrderDetailsDto>(OrderFulfillmentLegErrors.Forbidden);
        }

        var dto = new PharmacistOrderDetailsDto
        {
            OrderId = order.OrderId,
            OrderNumber = $"ORD-{order.OrderId.ToString().Substring(0, 8).ToUpper()}",
            CreatedAt = order.CreatedAt,
            TotalAmount = order.TotalAmount,
            OrderStatus = order.OrderStatus,
            FulfillmentMode = order.FulfillmentMode,
            Patient = new PharmacistOrderPatientDto
            {
                PatientId = order.PatientUserId,
                FullName = order.Patient.FullName,
                PhoneNumber = order.Patient.PhoneNumber ?? string.Empty
            },
            Items = order.Items.Where(i => i.BranchId == assignedLeg.BranchId).Select(i => new PharmacistOrderItemDto
            {
                DrugId = i.DrugId,
                DrugName = !string.IsNullOrWhiteSpace(i.Drug.BrandName) ? i.Drug.BrandName : i.Drug.ArabicName,
                ImageUrl = i.Drug.ImageUrl,
                Quantity = i.QuantityNeeded,
                DosageForm = i.Drug.Form
            }).ToList(),
            Notes = order.PrescriptionReview?.ReviewNotes,
            AssignedLeg = ToDto(assignedLeg)
        };

        return Result.Success(dto);
    }


    private static double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        var r = 6371;
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Asin(Math.Min(1, Math.Sqrt(a)));
        return r * c;
    }

    private static double ToRadians(double angle) => Math.PI * angle / 180.0;
}