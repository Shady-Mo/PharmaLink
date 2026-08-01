using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace Infrastructure.AI.Plugins;

/// <summary>
/// Native SK plugin that gives the AI read-only access to order data.
/// Allows patients to ask about their order status conversationally.
/// </summary>
public sealed class OrderPlugin(IServiceScopeFactory scopeFactory, ILogger<OrderPlugin> logger)
{
    public sealed record OrderStatusResult(
        bool Found,
        string? Message = null,
        OrderDetail? Order = null
    );

    public sealed record OrderDetail(
        Guid Id,
        string Status,
        DateTimeOffset CreatedAt,
        IReadOnlyList<OrderItem> Items,
        IReadOnlyList<OrderFulfillmentLeg> FulfillmentLegs
    );

    public sealed record OrderItem(
        string DrugName,
        int Quantity
    );

    public sealed record OrderFulfillmentLeg(
        string Status,
        string BranchName
    );

    [KernelFunction("get_order_status")]
    [Description(
        "Retrieves the current status of a patient's order by order ID. " +
        "Returns the order status, items, and fulfillment legs. " +
        "Use this when the user asks 'What is the status of my order?' or 'Where is my order?'")]
    public async Task<OrderStatusResult> GetOrderStatusAsync(
        [Description("The order ID (GUID) to look up.")]
        Guid orderId,
        [Description("The patient's user ID — used to ensure users can only see their own orders.")]
        Guid patientUserId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "OrderPlugin.GetOrderStatusAsync for Order {OrderId}, Patient {PatientId}",
            orderId, patientUserId);

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Security: always filter by both orderId AND patientUserId
        // so a user cannot query another user's order.
        var order = await db.Orders
            .AsNoTracking()
            .Include(o => o.Items).ThenInclude(i => i.Drug)
            .Include(o => o.FulfillmentLegs).ThenInclude(l => l.Branch)
            .Where(o => o.OrderId == orderId && o.PatientUserId == patientUserId)
            .Select(o => new OrderDetail(
                o.OrderId,
                o.OrderStatus.ToString(),
                o.CreatedAt,
                o.Items.Select(i => new OrderItem(
                    i.Drug.BrandName,
                    i.QuantityNeeded
                )).ToList(),
                o.FulfillmentLegs.Select(l => new OrderFulfillmentLeg(
                    l.LegStatus.ToString(),
                    l.Branch != null ? l.Branch.BranchName : "N/A"
                )).ToList()
            ))
            .FirstOrDefaultAsync(cancellationToken);

        if (order is null)
            return new OrderStatusResult(Found: false, Message: "Order not found, or you do not have permission to view this order.");

        return new OrderStatusResult(Found: true, Order: order);
    }
}