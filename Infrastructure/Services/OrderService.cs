using Application.DTOs.Order.Requests;
using Application.DTOs.Order.Responses;
using Application.DTOs.OrderRouting;
using System.Text;
using System.IO;
using Microsoft.Extensions.Configuration;
using Application.Services;
using Application.Abstractions;

namespace Infrastructure.Services;

public class OrderService(
    AppDbContext context,
    IOrderSplittingService orderSplittingService,
    CartCacheService cartCacheService,
    IInventoryService inventoryService,
    IWebPushNotificationService webPushService,
    IStripePaymentService stripePaymentService,
    IConfiguration configuration) : IOrderService
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
            .Select(d => new { d.DrugId, d.RequiresPrescription })
            .ToListAsync();

        var invalidDrugIds = drugIds.Except(existingDrugs.Select(d => d.DrugId)).ToList();
        if (invalidDrugIds.Count > 0)
            return Result.Failure<OrderCreatedResponseDTO>(
                OrderErrors.CreateInvalidDrugIdsError(invalidDrugIds));

        bool requiresPrescription = existingDrugs.Any(d => d.RequiresPrescription);
        Prescription? validPrescription = null;

        if (requiresPrescription)
        {
            if (!createOrderDTO.TemporaryPrescriptionId.HasValue)
            {
                return Result.Failure<OrderCreatedResponseDTO>(OrderErrors.PrescriptionRequired);
            }

            validPrescription = await context.Prescriptions
                .FirstOrDefaultAsync(p => p.Id == createOrderDTO.TemporaryPrescriptionId.Value && p.PatientId == patientUserId);

            if (validPrescription == null)
            {
                return Result.Failure<OrderCreatedResponseDTO>(OrderErrors.InvalidPrescription);
            }
        }

        var totalAmount = await CalculateTotalAmount(cartItems);

        var order = new Order
        {
            OrderId = Guid.NewGuid(),
            PatientUserId = patientUserId,
            DeliveryAddressId = createOrderDTO.DeliveryAddressId,
            FulfillmentMode = createOrderDTO.FulfillmentMode,
            OrderStatus = OrderStatus.Pending,
            TotalAmount = totalAmount,
            PaymentMethod = createOrderDTO.PaymentMethod,
            PaymentStatus = PaymentStatus.Pending
        };

        if (requiresPrescription && validPrescription != null)
        {
            validPrescription.OrderId = order.OrderId;
            validPrescription.Status = PrescriptionStatus.PendingReview;
            context.Prescriptions.Update(validPrescription);
            
            // Set the order status to PendingPrescriptionReview so it's not processed until approved
            order.OrderStatus = OrderStatus.PendingPrescriptionReview;
        }

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

        // Trigger automatic splitting inline and capture the resulting fulfillment plan. The plan
        // lets the 201 Created response surface — per pharmacy branch — the group of drugs that
        // branch supplies (Arabic + English names) and its distance from the patient, plus any
        // requested items no nearby branch could fulfill.
        var splitResult = await orderSplittingService.SplitOrderAsync(order.OrderId);
        var plan = splitResult.IsSuccess ? splitResult.Value : null;

        // Persist the AI routing explanation so the frontend can display it when fetching the order.
        // Prefer the AI's rich, ordered route description; fall back to its short strategy reasoning.
        // The split ran on its own DbContext, so re-fetch the (now tracked) order to update just this
        // column without clobbering the status the split advanced to Processing.
        var aiRoutingDescription = !string.IsNullOrWhiteSpace(plan?.RouteSummary?.Description)
            ? plan!.RouteSummary!.Description
            : plan?.Reasoning;

        if (!string.IsNullOrWhiteSpace(aiRoutingDescription))
        {
            var persistedOrder = await context.Orders.FirstOrDefaultAsync(o => o.OrderId == order.OrderId);
            if (persistedOrder is not null)
            {
                persistedOrder.AiRoutingDescription = aiRoutingDescription;
                await context.SaveChangesAsync();
            }
        }

        // Read the persisted status back (the split advances it to Processing on success).
        var finalStatus = await context.Orders
            .AsNoTracking()
            .Where(o => o.OrderId == order.OrderId)
            .Select(o => o.OrderStatus)
            .FirstOrDefaultAsync();


        var response = BuildOrderCreatedResponse(order.OrderId, finalStatus, plan);

        if (order.PaymentMethod == PaymentMethod.Stripe)
        {
            var baseUrl = configuration["Frontend:BaseUrl"] ?? "http://localhost:4200";
            var successUrl = $"{baseUrl}/patient/checkout/payment/success";
            var cancelUrl = $"{baseUrl}/patient/checkout/payment/cancel";
            
            var sessionUrl = await stripePaymentService.CreateCheckoutSessionAsync(order, successUrl, cancelUrl);
            
            context.Orders.Update(order);

            var paymentTransaction = new PaymentTransaction
            {
                OrderId = order.OrderId,
                StripeSessionId = order.StripeSessionId,
                StripePaymentIntentId = order.StripePaymentIntentId,
                Amount = order.TotalAmount,
                Currency = "egp",
                Status = "Created",
                EventType = "checkout.session.created",
                CreatedAt = DateTime.UtcNow
            };
            context.PaymentTransactions.Add(paymentTransaction);

            await context.SaveChangesAsync();
            
            response.PaymentUrl = sessionUrl;
        }

        return Result.Success<OrderCreatedResponseDTO>(response);
    }

    /// <summary>
    /// Maps the fulfillment <see cref="OrderRoutingPlan"/> (or its absence) into the patient-facing
    /// <see cref="OrderCreatedResponseDTO"/>: available drug groups per branch with distances and
    /// bilingual drug names, plus the unavailable items.
    /// </summary>
    private static OrderCreatedResponseDTO BuildOrderCreatedResponse(
        Guid orderId, OrderStatus status, OrderRoutingPlan? plan)
    {
        var groups = plan?.Legs.Select(leg => new OrderFulfillmentGroupDTO
        {
            PharmacyId = leg.PharmacyId,
            BranchId = leg.BranchId,
            BranchName = leg.BranchName,
            DistanceKm = leg.DistanceKm,
            Subtotal = leg.LegSubtotal,
            Items = leg.Items.Select(i => new OrderItemLineDTO
            {
                DrugId = i.DrugId,
                DrugName = i.DrugName,
                DrugNameAr = i.DrugNameAr,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                LineTotal = i.LineTotal
            }).ToList()
        }).ToList() ?? [];

        var unavailable = plan?.UnfulfillableItems.Select(m => new UnavailableItemDTO
        {
            DrugId = m.DrugId,
            DrugName = m.DrugName,
            DrugNameAr = m.DrugNameAr,
            QuantityNeeded = m.QuantityNeeded,
            QuantityAvailable = m.QuantityAvailable
        }).ToList() ?? [];

        var hasGroups = groups.Count > 0;
        var hasUnavailable = unavailable.Count > 0;

        string message;
        bool isFullyFulfilled;
        if (!hasGroups && !hasUnavailable)
        {
            message = "Order created successfully and is awaiting fulfillment assignment.";
            isFullyFulfilled = false;
        }
        else if (hasGroups && !hasUnavailable)
        {
            message = "Order created successfully. All items are available and grouped by pharmacy branch.";
            isFullyFulfilled = true;
        }
        else if (!hasGroups)
        {
            message = "Order created, but no items could be fulfilled by nearby pharmacies at the moment.";
            isFullyFulfilled = false;
        }
        else
        {
            message = "Order created successfully. Some items are available and grouped by pharmacy branch; others are currently unavailable.";
            isFullyFulfilled = false;
        }

        return new OrderCreatedResponseDTO
        {
            OrderId = orderId,
            Status = status,
            Message = message,
            Strategy = plan?.Strategy ?? string.Empty,
            IsFullyFulfilled = isFullyFulfilled,
            TotalDistanceKm = plan?.TotalDistanceKm ?? 0,
            FulfillmentGroups = groups,
            UnavailableItems = unavailable
        };
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
        var baseQuery = context.Orders.AsNoTracking();

        // 1. Search (Patient FullName or OrderNumber/OrderId)
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            baseQuery = baseQuery.Where(o =>
                o.Patient.FullName.ToLower().Contains(search) ||
                o.OrderId.ToString().ToLower().Contains(search));
        }

        // 2. Status filter
        if (request.Status.HasValue)
        {
            if ((int)request.Status.Value == 100)
            {
                baseQuery = baseQuery.Where(o => o.OrderStatus == OrderStatus.Pending ||
                                                 o.OrderStatus == OrderStatus.Processing ||
                                                 o.OrderStatus == OrderStatus.Shipped);
            }
            else
            {
                baseQuery = baseQuery.Where(o => o.OrderStatus == request.Status.Value);
            }
        }

        if (request.FulfillmentMode.HasValue)
        {
            baseQuery = baseQuery.Where(o => o.FulfillmentMode == request.FulfillmentMode.Value);
        }

        if (request.LegStatus.HasValue)
        {
            baseQuery = baseQuery.Where(o => o.FulfillmentLegs.Any(l => l.LegStatus == request.LegStatus.Value));
        }

        // 3. Date range filter
        if (request.FromDate.HasValue)
        {
            baseQuery = baseQuery.Where(o => o.CreatedAt >= request.FromDate.Value);
        }
        if (request.ToDate.HasValue)
        {
            baseQuery = baseQuery.Where(o => o.CreatedAt <= request.ToDate.Value);
        }

        // 4. Fast Count (executed directly on base table without joins)
        var count = await baseQuery.CountAsync(ct);

        // 5. Sorting
        baseQuery = request.SortBy.ToLower() switch
        {
            "amount" => request.SortDir.ToLower() == "asc"
                ? baseQuery.OrderBy(o => o.TotalAmount)
                : baseQuery.OrderByDescending(o => o.TotalAmount),
            "status" => request.SortDir.ToLower() == "asc"
                ? baseQuery.OrderBy(o => o.OrderStatus)
                : baseQuery.OrderByDescending(o => o.OrderStatus),
            _ => request.SortDir.ToLower() == "asc"
                ? baseQuery.OrderBy(o => o.CreatedAt)
                : baseQuery.OrderByDescending(o => o.CreatedAt)
        };

        // 6. Paginate first, then split query includes for ONLY the 10 paginated orders
        var orders = await baseQuery
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Include(o => o.Patient)
            .Include(o => o.Items)
                .ThenInclude(i => i.Drug)
            .Include(o => o.FulfillmentLegs)
                .ThenInclude(l => l.Branch)
                    .ThenInclude(b => b.Pharmacy)
            .AsSplitQuery()
            .ToListAsync(ct);

        var list = orders.Select(o => new AdminOrderDTO
        {
            OrderId = o.OrderId,
            OrderNumber = "ORD-" + o.OrderId.ToString().Substring(0, 8).ToUpper(),
            PatientName = o.Patient?.FullName ?? "Unknown",
            PrimaryPharmacyName = o.FulfillmentLegs.FirstOrDefault()?.Branch?.Pharmacy?.LegalName ?? "لم يتم التعيين",
            MedicineNames = o.Items.Select(i => i.Drug?.BrandName ?? "Unknown").Distinct().ToList(),
            TotalAmount = o.TotalAmount,
            OrderStatus = o.OrderStatus,
            FulfillmentMode = o.FulfillmentMode,
            CreatedAt = o.CreatedAt,
            ItemCount = o.Items.Count,
            LegStatus = o.FulfillmentLegs.FirstOrDefault()?.LegStatus
        }).ToList();

        return Result.Success(new PaginatedList<AdminOrderDTO>(list, request.PageNumber, count, request.PageSize));
    }

    public async Task<Result<AdminOrderDetailDTO>> GetAdminOrderDetail(Guid orderId, CancellationToken ct = default)
    {
        var order = await context.Orders
            .Include(o => o.Patient)
            .Include(o => o.DeliveryAddress)
            .Include(o => o.PrescriptionReview)
            .Include(o => o.Prescription)
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
            HasPrescription = order.PrescriptionReview != null || order.Prescription != null,
            PrescriptionId = order.PrescriptionReview?.PrescriptionReviewId ?? order.Prescription?.Id,
            DeliveryAddress = order.DeliveryAddress != null
                ? $"{order.DeliveryAddress.Governorate}، {order.DeliveryAddress.City}، {order.DeliveryAddress.AddressLine}"
                : "No Address",
            Items = order.Items.Select(i => new AdminOrderItemDTO
            {
                OrderItemId = i.OrderItemId,
                DrugId = i.DrugId,
                DrugName = i.Drug?.BrandName ?? "Unknown",
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

        //if (request.FulfillmentMode.HasValue)
        //{
        //    query = query.Where(o => o.FulfillmentMode == request.FulfillmentMode.Value);
        //}

        //if (request.LegStatus.HasValue)
        //{
        //    query = query.Where(o => o.FulfillmentLegs.Any(l => l.LegStatus == request.LegStatus.Value));
        //}

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

    public async Task<Result<string>> ApproveOrderPrescription(Guid orderId, CancellationToken ct = default)
    {
        var order = await context.Orders
            .FirstOrDefaultAsync(o => o.OrderId == orderId, ct);

        if (order == null)
            return Result.Failure<string>(OrderErrors.OrderNotFound);

        var prescription = await context.Prescriptions
            .FirstOrDefaultAsync(p => p.OrderId == orderId, ct);

        if (prescription == null)
            return Result.Failure<string>(OrderErrors.PrescriptionRequired);

        if (order.OrderStatus != OrderStatus.PendingPrescriptionReview)
            return Result.Failure<string>(OrderErrors.OrderCannotBeModified);

        prescription.Status = PrescriptionStatus.Approved;
        context.Prescriptions.Update(prescription);

        order.OrderStatus = OrderStatus.Pending; // Return it to normal pending state so it can be fulfilled
        
        await context.SaveChangesAsync(ct);
        await webPushService.SendNotificationAsync(order.PatientUserId, "تم قبول الروشتة ✅", "تمت مراجعة الروشتة الخاصة بطلبك وقبولها بنجاح. جاري تجهيز الطلب.", $"/patient/orders/{order.OrderId}");
        return Result.Success("Success");
    }

    public async Task<Result<string>> RejectOrderPrescription(Guid orderId, string reason, CancellationToken ct = default)
    {
        var order = await context.Orders
            .FirstOrDefaultAsync(o => o.OrderId == orderId, ct);

        if (order == null)
            return Result.Failure<string>(OrderErrors.OrderNotFound);

        var prescription = await context.Prescriptions
            .FirstOrDefaultAsync(p => p.OrderId == orderId, ct);

        if (prescription == null)
            return Result.Failure<string>(OrderErrors.PrescriptionRequired);

        if (order.OrderStatus != OrderStatus.PendingPrescriptionReview)
            return Result.Failure<string>(OrderErrors.OrderCannotBeModified);

        prescription.Status = PrescriptionStatus.Rejected;
        prescription.RejectionReason = reason;
        context.Prescriptions.Update(prescription);

        order.OrderStatus = OrderStatus.Cancelled; // If prescription is rejected, order cannot proceed
        
        await context.SaveChangesAsync(ct);
        await webPushService.SendNotificationAsync(order.PatientUserId, "تم رفض الروشتة ❌", $"عذراً، تم رفض الروشتة المرفقة بطلبك. السبب: {reason}", $"/patient/orders/{order.OrderId}");
        return Result.Success("Success");
    }

    public async Task<Result<string>> RemoveOrderItemAsync(Guid orderId, Guid orderItemId, CancellationToken ct = default)
    {
        var order = await context.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.Drug)
            .FirstOrDefaultAsync(o => o.OrderId == orderId, ct);

        if (order == null)
            return Result.Failure<string>(OrderErrors.OrderNotFound);

        if (order.OrderStatus != OrderStatus.PendingPrescriptionReview && order.OrderStatus != OrderStatus.Pending)
            return Result.Failure<string>(OrderErrors.OrderCannotBeModified);

        var itemToRemove = order.Items.FirstOrDefault(i => i.OrderItemId == orderItemId);
        if (itemToRemove == null)
            return Result.Failure<string>(OrderErrors.OrderNotFound); // Or OrderItemNotFound if it existed

        // Subtract from TotalAmount
        var lineTotal = itemToRemove.QuantityNeeded * itemToRemove.Drug.Price;
        order.TotalAmount -= lineTotal;
        if (order.TotalAmount < 0) order.TotalAmount = 0;

        context.OrderItems.Remove(itemToRemove);
        
        // If it's the last item, maybe cancel the order?
        if (order.Items.Count == 1)
        {
            order.OrderStatus = OrderStatus.Cancelled;
        }

        await context.SaveChangesAsync(ct);
        return Result.Success("Success");
    }

    public async Task<Result<string>> CancelOrder(Guid orderId, Guid patientUserId, CancellationToken ct = default)
    {
        var order = await context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderId == orderId && o.PatientUserId == patientUserId, ct);

        if (order == null)
            return Result.Failure<string>(OrderErrors.OrderNotFound);

        if (order.OrderStatus != OrderStatus.Pending && 
            order.OrderStatus != OrderStatus.Processing &&
            order.OrderStatus != OrderStatus.PendingPrescriptionReview)
        {
            return Result.Failure<string>(OrderErrors.OrderCannotBeModified);
        }

        var awardedItems = order.Items.Where(i => i.ItemStatus == ItemStatus.Awarded && i.BranchId.HasValue).ToList();
        var releases = awardedItems
            .GroupBy(i => new { i.BranchId, i.DrugId })
            .Select(g => (g.Key.BranchId!.Value, g.Key.DrugId, g.Sum(x => x.QuantityNeeded)))
            .ToList();

        if (releases.Any())
        {
            var releaseResult = await inventoryService.ReleaseReservationBatchAsync(releases, ct);
        }

        foreach (var item in order.Items)
        {
            item.ItemStatus = ItemStatus.Cancelled;
        }
        
        order.OrderStatus = OrderStatus.Cancelled;

        var prescription = await context.Prescriptions
            .FirstOrDefaultAsync(p => p.OrderId == orderId, ct);
        if (prescription != null)
        {
            prescription.Status = PrescriptionStatus.Rejected;
            prescription.RejectionReason = "Cancelled by patient";
            context.Prescriptions.Update(prescription);
        }

        await context.SaveChangesAsync(ct);
        return Result.Success("Order cancelled successfully");
    }
}

