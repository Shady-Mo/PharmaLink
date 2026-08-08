namespace Infrastructure.Services;

public class PharmacyOrderService(
    AppDbContext context,
    ILogger<PharmacyOrderService> logger) : IPharmacyOrderService
{
    public async Task<Result<PaginatedList<PharmacyOrderSummaryDTO>>> GetOrdersAsync(
        Guid pharmacyId,
        OrderQueryParametersDto query,
        CancellationToken cancellationToken = default)
    {
        var legsQuery = context.OrderFulfillmentLegs
            .AsNoTracking()
            .Where(l => l.Branch.PharmacyId == pharmacyId);

        if (query.BranchId.HasValue)
            legsQuery = legsQuery.Where(l => l.BranchId == query.BranchId.Value);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            var idTerm = term.StartsWith("ord-") ? term[4..] : term;

            legsQuery = legsQuery.Where(l =>
                l.Order.Patient.FullName.ToLower().Contains(term) ||
                l.OrderId.ToString().ToLower().Contains(idTerm));
        }

        if (query.Status.HasValue)
            legsQuery = legsQuery.Where(l => l.LegStatus == query.Status.Value);

        if (query.OrderDateFrom.HasValue)
            legsQuery = legsQuery.Where(l => l.Order.CreatedAt >= query.OrderDateFrom.Value);

        if (query.OrderDateTo.HasValue)
            legsQuery = legsQuery.Where(l => l.Order.CreatedAt <= query.OrderDateTo.Value);

        if (query.DeliveryDateFrom.HasValue)
            legsQuery = legsQuery.Where(l => l.Order.DeliveredAt != null && l.Order.DeliveredAt >= query.DeliveryDateFrom.Value);

        if (query.DeliveryDateTo.HasValue)
            legsQuery = legsQuery.Where(l => l.Order.DeliveredAt != null && l.Order.DeliveredAt <= query.DeliveryDateTo.Value);

        var projected = legsQuery.Select(l => new PharmacyOrderSummaryDTO
        {
            OrderId = l.OrderId,
            OrderNumber = "ORD-" + l.OrderId.ToString().Substring(0, 8).ToUpper(),
            PatientName = l.Order.Patient.FullName,
            OrderDate = l.Order.CreatedAt,
            DeliveryDate = l.CompletedAt,
            LegStatus = l.LegStatus,
            FulfillmentMode = l.Order.FulfillmentMode,
            TotalAmount = l.Order.TotalAmount,
            ItemsCount = l.Order.Items.Count(i =>
                i.BranchId != null &&
                (query.BranchId != null
                    ? i.BranchId == query.BranchId
                    : i.Branch.PharmacyId == pharmacyId))
        });

        projected = query.SortBy switch
        {
            PharmacyOrderSort.OldestFirst => projected.OrderBy(x => x.OrderDate),
            PharmacyOrderSort.HighestAmount => projected.OrderByDescending(x => x.TotalAmount),
            PharmacyOrderSort.LowestAmount => projected.OrderBy(x => x.TotalAmount),
            _ => projected.OrderByDescending(x => x.OrderDate)
        };

        var paginated = await projected.ToPaginatedListAsync(query.PageNumber, query.PageSize, cancellationToken);
        return Result.Success(paginated);
    }

    public async Task<Result<PharmacyOrderDetailDTO>> GetOrderByIdAsync(
        Guid pharmacyId,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var leg = await context.OrderFulfillmentLegs
            .AsNoTracking()
            .Where(l => l.OrderId == orderId && l.Branch.PharmacyId == pharmacyId)
            .Select(l => new PharmacyOrderDetailDTO
            {
                OrderId = l.OrderId,
                OrderNumber = "ORD-" + l.OrderId.ToString().Substring(0, 8).ToUpper(),
                LegStatus = l.LegStatus,
                FulfillmentMode = l.Order.FulfillmentMode,
                OrderDate = l.Order.CreatedAt,
                DeliveryDate = l.CompletedAt,
                HasPrescription = l.Order.Prescription != null,
                PrescriptionId = l.Order.Prescription != null ? l.Order.Prescription.Id : null,
                TotalAmount = l.Order.TotalAmount,

                Patient = new PharmacyOrderPatientDTO
                {
                    PatientUserId = l.Order.PatientUserId,
                    FullName = l.Order.Patient.FullName,
                    Email = l.Order.Patient.Email,
                    PhoneNumber = l.Order.Patient.PhoneNumber
                },

                DeliveryAddress = new PharmacyOrderAddressDTO
                {
                    AddressLine = l.Order.DeliveryAddress.AddressLine,
                    City = l.Order.DeliveryAddress.City,
                    Governorate = l.Order.DeliveryAddress.Governorate
                },

                Items = l.Order.Items
                    .Where(i => i.BranchId != null && i.Branch.PharmacyId == pharmacyId)
                    .Select(i => new PharmacyOrderItemDTO
                    {
                        OrderItemId = i.OrderItemId,
                        DrugId = i.DrugId,
                        DrugName = i.Drug.BrandName,
                        ArabicName = i.Drug.ArabicName,
                        Strength = i.Drug.Strength,
                        Form = i.Drug.Form,
                        Quantity = i.QuantityNeeded,
                        UnitPrice = i.Drug.Price,
                        ItemStatus = i.ItemStatus
                    }).ToList(),

                FulfillmentLegs = l.Order.FulfillmentLegs
                    .Where(fl => fl.Branch.PharmacyId == pharmacyId)
                    .Select(fl => new PharmacyOrderLegDTO
                    {
                        LegId = fl.LegId,
                        BranchId = fl.BranchId,
                        BranchName = fl.Branch.BranchName,
                        LegType = fl.LegType,
                        LegStatus = fl.LegStatus,
                        ReadyByEstimate = fl.ReadyByEstimate,
                        CompletedAt = fl.CompletedAt
                    }).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (leg is null)
        {
            logger.LogWarning(
                "Pharmacy {PharmacyId} attempted to access order {OrderId} outside its scope or that does not exist.",
                pharmacyId, orderId);
            return Result.Failure<PharmacyOrderDetailDTO>(PharmacyOrderErrors.OrderNotFound);
        }

        return Result.Success(leg);
    }
}
