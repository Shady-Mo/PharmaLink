using Application.DTOs.Order.Requests;
using Application.DTOs.Order.Responses;
namespace Infrastructure.Services;

public class OrderService(
    AppDbContext context,
    IOrderSplittingService orderSplittingService,
    CartCacheService cartCacheService) : IOrderService
{
    public async Task<Result<OrderCreatedResponseDTO>> CreateOrder(Guid patientUserId,
        CreateOrderDTO createOrderDTO)
    {
        var cart = await context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.PatientUserId == patientUserId);

        if (cart is null || cart.Items.Count == 0)
            return Result.Failure<OrderCreatedResponseDTO>(OrderErrors.OrderMustContainItems);

        var address = await context.Addresses
            .FirstOrDefaultAsync(a => a.AddressId == createOrderDTO.DeliveryAddressId
                                      && a.UserId == patientUserId);

        if (address is null)
            return Result.Failure<OrderCreatedResponseDTO>(OrderErrors.InvalidDeliveryAddress);

        var cartItems = cart.Items
            .Select(ci => new OrderItemRequestDTO { DrugId = ci.DrugId, QuantityNeeded = ci.Quantity })
            .ToList();

        var drugIds = cartItems.Select(item => item.DrugId).Distinct().ToList();
        var existingDrugs = await context.Drugs
            .Where(d => drugIds.Contains(d.DrugId))
            .Select(d => d.DrugId)
            .ToListAsync();

        var invalidDrugIds = drugIds.Except(existingDrugs).ToList();
        if (invalidDrugIds.Count > 0)
            return Result.Failure<OrderCreatedResponseDTO>(
                OrderErrors.CreateInvalidDrugIdsError(invalidDrugIds));

        var totalAmount = await CalculateTotalAmount(cartItems);

        var order = new Order
        {
            OrderId = Guid.NewGuid(),
            PatientUserId = patientUserId,
            DeliveryAddressId = createOrderDTO.DeliveryAddressId,
            FulfillmentMode = createOrderDTO.FulfillmentMode,
            OrderStatus = OrderStatus.Pending,
            TotalAmount = totalAmount
        };

        context.Orders.Add(order);

        var orderItems = cartItems.Select(item => new OrderItem
        {
            OrderItemId = Guid.NewGuid(),
            OrderId = order.OrderId,
            DrugId = item.DrugId,
            QuantityNeeded = item.QuantityNeeded,
            ItemStatus = ItemStatus.Pending,
            BranchId = null
        }).ToList();

        context.OrderItems.AddRange(orderItems);

        // Clear the cart now that its contents have been committed to the order.
        context.CartItems.RemoveRange(cart.Items);

        await context.SaveChangesAsync();

        // Invalidate the Redis cache so a subsequent GetCart doesn't serve stale, pre-checkout items.
        await cartCacheService.InvalidateAsync(patientUserId);

        // Trigger automatic splitting inline. Result is observed but not propagated to caller —
        // the 201 Created response represents the order being accepted, not fully split.
        await orderSplittingService.SplitOrderAsync(order.OrderId);

        var response = new OrderCreatedResponseDTO
        {
            OrderId = order.OrderId,
            Status = order.OrderStatus,
            Message = "Order created successfully and is awaiting fulfillment assignment."
        };

        return Result.Success<OrderCreatedResponseDTO>(response);
    }

    public async Task<Result<GetOrderDTO>> GetOrder(Guid orderId, Guid patientUserId)
    {
        var dto = await context.Orders
            .Where(o => o.OrderId == orderId && o.PatientUserId == patientUserId)
            .ProjectToType<GetOrderDTO>()
            .FirstOrDefaultAsync();

        if (dto is null)
            return Result.Failure<GetOrderDTO>(OrderErrors.OrderNotFound);

        return Result.Success<GetOrderDTO>(dto);
    }

    public async Task<Result<PaginatedList<GetOrderDTO>>> GetOrders(Guid patientUserId, GetOrdersRequest request)
    {
        var paginatedResult = await context.Orders
            .Where(o => o.PatientUserId == patientUserId)
            .OrderByDescending(o => o.CreatedAt)
            .ProjectToType<GetOrderDTO>()
            .ToPaginatedListAsync(request.PageNumber, request.PageSize);

        return Result.Success(paginatedResult);
    }

    public async Task<Result<GetOrderDTO>> GetOrderForAdmin(Guid orderId)
    {
        var dto = await context.Orders
            .Where(o => o.OrderId == orderId)
            .ProjectToType<GetOrderDTO>()
            .FirstOrDefaultAsync();

        if (dto is null)
            return Result.Failure<GetOrderDTO>(OrderErrors.OrderNotFound);

        return Result.Success(dto);
    }

    public async Task<Result<PaginatedList<GetOrderDTO>>> GetOrdersForAdmin(GetOrdersRequest request)
    {
        var paginatedResult = await context.Orders
            .OrderByDescending(o => o.CreatedAt)
            .ProjectToType<GetOrderDTO>()
            .ToPaginatedListAsync(request.PageNumber, request.PageSize);

        return Result.Success(paginatedResult);
    }

    private async Task<decimal> CalculateTotalAmount(ICollection<OrderItemRequestDTO> items)
    {
        if (items is null || items.Count == 0)
            return 0;

        var drugIds = items.Select(i => i.DrugId).Distinct().ToList();

        var drugPrices = await context.Drugs
            .Where(d => drugIds.Contains(d.DrugId))
            .Select(d => new
            {
                DrugId = d.DrugId,
                UnitPrice = d.Price
            }).ToListAsync();

        decimal total = 0;
        foreach (var item in items)
        {
            var drugPrice = drugPrices.FirstOrDefault(dp => dp.DrugId == item.DrugId);
            if (drugPrice is not null)
            {
                total += drugPrice.UnitPrice * item.QuantityNeeded;
            }
        }

        return total;
    }

    public async Task<Result<PaginatedList<AdminOrderDTO>>> GetAdminOrders(GetOrdersRequest request, CancellationToken ct = default)
    {
        var query = context.Orders
            .Include(o => o.Patient)
            .Include(o => o.Items)
                .ThenInclude(i => i.Drug)
            .Include(o => o.FulfillmentLegs)
                .ThenInclude(l => l.Branch)
                    .ThenInclude(b => b.Pharmacy)
            .AsNoTracking();

        // 1. Search (Patient FullName or OrderNumber/OrderId)
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(o => 
                o.Patient.FullName.ToLower().Contains(search) ||
                o.OrderId.ToString().ToLower().Contains(search));
        }

        // 2. Status filter
        if (request.Status.HasValue)
        {
            if ((int)request.Status.Value == 100)
            {
                query = query.Where(o => o.OrderStatus == OrderStatus.Pending || 
                                         o.OrderStatus == OrderStatus.Processing || 
                                         o.OrderStatus == OrderStatus.Shipped);
            }
            else
            {
                query = query.Where(o => o.OrderStatus == request.Status.Value);
            }
        }

        // 3. Date range filter
        if (request.FromDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt >= request.FromDate.Value);
        }
        if (request.ToDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt <= request.ToDate.Value);
        }

        // 4. Sorting
        query = request.SortBy.ToLower() switch
        {
            "amount" => request.SortDir.ToLower() == "asc" 
                ? query.OrderBy(o => o.TotalAmount) 
                : query.OrderByDescending(o => o.TotalAmount),
            "status" => request.SortDir.ToLower() == "asc" 
                ? query.OrderBy(o => o.OrderStatus) 
                : query.OrderByDescending(o => o.OrderStatus),
            _ => request.SortDir.ToLower() == "asc" 
                ? query.OrderBy(o => o.CreatedAt) 
                : query.OrderByDescending(o => o.CreatedAt)
        };

        // 5. Pagination
        var count = await query.CountAsync(ct);
        var orders = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var list = orders.Select(o => new AdminOrderDTO
        {
            OrderId = o.OrderId,
            OrderNumber = "ORD-" + o.OrderId.ToString().Substring(0, 8).ToUpper(),
            PatientName = o.Patient?.FullName ?? "Unknown",
            PrimaryPharmacyName = o.FulfillmentLegs.FirstOrDefault()?.Branch?.Pharmacy?.LegalName ?? "Not Assigned",
            MedicineNames = o.Items.Select(i => i.Drug?.BrandName ?? "Unknown").Distinct().ToList(),
            TotalAmount = o.TotalAmount,
            OrderStatus = o.OrderStatus,
            FulfillmentMode = o.FulfillmentMode,
            CreatedAt = o.CreatedAt,
            ItemCount = o.Items.Count
        }).ToList();

        return Result.Success(new PaginatedList<AdminOrderDTO>(list, request.PageNumber, count, request.PageSize));
    }

    public async Task<Result<AdminOrderDetailDTO>> GetAdminOrderDetail(Guid orderId, CancellationToken ct = default)
    {
        var order = await context.Orders
            .Include(o => o.Patient)
            .Include(o => o.DeliveryAddress)
            .Include(o => o.Items)
                .ThenInclude(i => i.Drug)
            .Include(o => o.FulfillmentLegs)
                .ThenInclude(l => l.Branch)
                    .ThenInclude(b => b.Pharmacy)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.OrderId == orderId, ct);

        if (order == null)
        {
            return Result.Failure<AdminOrderDetailDTO>(OrderErrors.OrderNotFound);
        }

        var dto = new AdminOrderDetailDTO
        {
            OrderId = order.OrderId,
            OrderNumber = "ORD-" + order.OrderId.ToString().Substring(0, 8).ToUpper(),
            PatientName = order.Patient?.FullName ?? "Unknown",
            PatientEmail = order.Patient?.Email ?? "Unknown",
            TotalAmount = order.TotalAmount,
            OrderStatus = order.OrderStatus,
            FulfillmentMode = order.FulfillmentMode,
            CreatedAt = order.CreatedAt,
            DeliveredAt = order.DeliveredAt,
            DeliveryAddress = order.DeliveryAddress != null 
                ? $"{order.DeliveryAddress.Governorate}، {order.DeliveryAddress.City}، {order.DeliveryAddress.AddressLine}"
                : "No Address",
            Items = order.Items.Select(i => new AdminOrderItemDTO
            {
                OrderItemId = i.OrderItemId,
                DrugId = i.DrugId,
                DrugName = i.Drug?.BrandName ?? "Unknown",
                GenericName = i.Drug?.GenericName,
                Strength = i.Drug?.Strength ?? string.Empty,
                DosageForm = i.Drug?.Form ?? string.Empty,
                QuantityNeeded = i.QuantityNeeded,
                UnitPrice = i.Drug?.Price ?? 0,
                ItemStatus = i.ItemStatus
            }).ToList(),
            FulfillmentLegs = order.FulfillmentLegs.Select(l => new AdminOrderLegDTO
            {
                LegId = l.LegId,
                LegType = l.LegType,
                LegStatus = l.LegStatus,
                PharmacyName = l.Branch?.Pharmacy?.LegalName ?? "Unknown",
                BranchName = l.Branch?.BranchName ?? "Unknown",
                City = l.Branch?.City ?? "Unknown",
                ReadyByEstimate = l.ReadyByEstimate,
                MedicineNames = order.Items
                    .Where(i => i.BranchId == l.BranchId)
                    .Select(i => i.Drug?.BrandName ?? "Unknown")
                    .ToList()
            }).ToList()
        };

        return Result.Success(dto);
    }

    public async Task<Result<(byte[] Data, string ContentType, string FileName)>> ExportOrdersForAdmin(
        ExportOrdersRequest request, CancellationToken ct = default)
    {
        var query = context.Orders
            .Include(o => o.Patient)
            .Include(o => o.Items)
                .ThenInclude(i => i.Drug)
            .Include(o => o.FulfillmentLegs)
                .ThenInclude(l => l.Branch)
                    .ThenInclude(b => b.Pharmacy)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(o => 
                o.Patient.FullName.ToLower().Contains(search) ||
                o.OrderId.ToString().ToLower().Contains(search));
        }

        if (request.Status.HasValue)
        {
            if ((int)request.Status.Value == 100)
            {
                query = query.Where(o => o.OrderStatus == OrderStatus.Pending || 
                                         o.OrderStatus == OrderStatus.Processing || 
                                         o.OrderStatus == OrderStatus.Shipped);
            }
            else
            {
                query = query.Where(o => o.OrderStatus == request.Status.Value);
            }
        }

        if (request.FromDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt >= request.FromDate.Value);
        }
        if (request.ToDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt <= request.ToDate.Value);
        }

        var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync(ct);

        var list = orders.Select(o => new
        {
            OrderNumber = "ORD-" + o.OrderId.ToString().Substring(0, 8).ToUpper(),
            PatientName = o.Patient?.FullName ?? "Unknown",
            Pharmacy = o.FulfillmentLegs.FirstOrDefault()?.Branch?.Pharmacy?.LegalName ?? "Not Assigned",
            Medicines = string.Join(", ", o.Items.Select(i => i.Drug?.BrandName ?? "Unknown").Distinct()),
            TotalAmount = o.TotalAmount,
            Status = o.OrderStatus.ToString(),
            Fulfillment = o.FulfillmentMode.ToString(),
            Date = o.CreatedAt.ToString("yyyy-MM-dd HH:mm")
        }).ToList();

        if (request.Format.ToLower() == "csv")
        {
            var csv = new StringBuilder();
            csv.AppendLine("Order Number,Patient Name,Pharmacy,Medicines,Total Amount,Status,Fulfillment,Date");
            foreach (var item in list)
            {
                var escapedPatient = item.PatientName.Contains(",") ? $"\"{item.PatientName}\"" : item.PatientName;
                var escapedPharmacy = item.Pharmacy.Contains(",") ? $"\"{item.Pharmacy}\"" : item.Pharmacy;
                var escapedMedicines = item.Medicines.Contains(",") ? $"\"{item.Medicines}\"" : item.Medicines;
                csv.AppendLine($"{item.OrderNumber},{escapedPatient},{escapedPharmacy},{escapedMedicines},{item.TotalAmount},{item.Status},{item.Fulfillment},{item.Date}");
            }
            var data = Encoding.UTF8.GetBytes(csv.ToString());
            return Result.Success((data, "text/csv", $"orders-export-{DateTime.UtcNow:yyyyMMddHHmmss}.csv"));
        }
        else
        {
            // Excel using ClosedXML
            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Orders");
            worksheet.Cell(1, 1).Value = "Order Number";
            worksheet.Cell(1, 2).Value = "Patient Name";
            worksheet.Cell(1, 3).Value = "Pharmacy";
            worksheet.Cell(1, 4).Value = "Medicines";
            worksheet.Cell(1, 5).Value = "Total Amount";
            worksheet.Cell(1, 6).Value = "Status";
            worksheet.Cell(1, 7).Value = "Fulfillment";
            worksheet.Cell(1, 8).Value = "Date";

            var headerRow = worksheet.Row(1);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#0F9D76");
            headerRow.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;

            for (int i = 0; i < list.Count; i++)
            {
                var row = i + 2;
                worksheet.Cell(row, 1).Value = list[i].OrderNumber;
                worksheet.Cell(row, 2).Value = list[i].PatientName;
                worksheet.Cell(row, 3).Value = list[i].Pharmacy;
                worksheet.Cell(row, 4).Value = list[i].Medicines;
                worksheet.Cell(row, 5).Value = list[i].TotalAmount;
                worksheet.Cell(row, 6).Value = list[i].Status;
                worksheet.Cell(row, 7).Value = list[i].Fulfillment;
                worksheet.Cell(row, 8).Value = list[i].Date;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var data = stream.ToArray();
            return Result.Success((data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"orders-export-{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx"));
        }
    }
}