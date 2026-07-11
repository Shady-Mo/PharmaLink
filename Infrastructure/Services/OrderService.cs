using Application.DTOs.Order.Requests;
using Application.DTOs.Order.Responses;
using Application.Services.Order;

namespace Infrastructure.Services;

public class OrderService(AppDbContext context) : IOrderService
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
        var order = await context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderId == orderId && o.PatientUserId == patientUserId);

        if (order is null)
            return Result.Failure<GetOrderDTO>(OrderErrors.OrderNotFound);

        var dto = order.Adapt<GetOrderDTO>();
        return Result.Success<GetOrderDTO>(dto);
    }

    public async Task<Result<PaginatedList<GetOrderDTO>>> GetOrders(Guid patientUserId, int pageNumber = 1,
        int pageSize = 10)
    {
        var query = context.Orders
            .Where(o => o.PatientUserId == patientUserId)
            .Include(o => o.Items)
            .OrderByDescending(o => o.OrderId);

        var totalCount = await query.CountAsync();

        var orders = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = orders.Adapt<List<GetOrderDTO>>();
        var paginatedResult = new PaginatedList<GetOrderDTO>(dtos, pageNumber, totalCount, pageSize);

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