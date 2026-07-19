using Application.DTOs.Order.Requests;
using Application.DTOs.Order.Responses;
using Application.Services.Order;

namespace Infrastructure.Services;

public class OrderService(AppDbContext context, IOrderSplittingService orderSplittingService) : IOrderService
{
    public async Task<Result<OrderCreatedResponseDTO>> CreateOrder(Guid patientUserId,
        CreateOrderDTO createOrderDTO)
    {
        if (createOrderDTO.Items is null || createOrderDTO.Items.Count == 0)
            return Result.Failure<OrderCreatedResponseDTO>(OrderErrors.OrderMustContainItems);

        var address = await context.Addresses
            .FirstOrDefaultAsync(a => a.AddressId == createOrderDTO.DeliveryAddressId
                                      && a.UserId == patientUserId);

        if (address is null)
            return Result.Failure<OrderCreatedResponseDTO>(OrderErrors.InvalidDeliveryAddress);

        var drugIds = createOrderDTO.Items.Select(item => item.DrugId).Distinct().ToList();
        var existingDrugs = await context.Drugs
            .Where(d => drugIds.Contains(d.DrugId))
            .Select(d => d.DrugId)
            .ToListAsync();

        var invalidDrugIds = drugIds.Except(existingDrugs).ToList();
        if (invalidDrugIds.Count > 0)
            return Result.Failure<OrderCreatedResponseDTO>(
                OrderErrors.CreateInvalidDrugIdsError(invalidDrugIds));

        var totalAmount = await CalculateTotalAmount(createOrderDTO.Items);

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

        var orderItems = createOrderDTO.Items.Select(item => new OrderItem
        {
            OrderItemId = Guid.NewGuid(),
            OrderId = order.OrderId,
            DrugId = item.DrugId,
            QuantityNeeded = item.QuantityNeeded,
            ItemStatus = ItemStatus.Pending,
            BranchId = null
        }).ToList();

        context.OrderItems.AddRange(orderItems);

        await context.SaveChangesAsync();

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
}