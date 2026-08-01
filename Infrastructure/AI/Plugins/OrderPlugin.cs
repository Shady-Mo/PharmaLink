using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace Infrastructure.AI.Plugins;

/// <summary>
/// Native SK plugin that gives the AI read-only access to order data.
/// Allows patients to ask about their order status conversationally.
/// </summary>
public sealed class OrderPlugin(IServiceScopeFactory scopeFactory, ILogger<OrderPlugin> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    [KernelFunction("get_order_status")]
    [Description(
        "Retrieves the current status of a patient's order by order ID. " +
        "Returns the order status, items, and fulfillment legs. " +
        "Use this when the user asks 'What is the status of my order?' or 'Where is my order?'")]
    public async Task<string> GetOrderStatusAsync(
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
            .Select(o => new
            {
                Id = o.OrderId,
                Status = o.OrderStatus.ToString(),
                o.CreatedAt,
                Items = o.Items.Select(i => new
                {
                    DrugName = i.Drug.BrandName,
                    Quantity = i.QuantityNeeded
                }),
                FulfillmentLegs = o.FulfillmentLegs.Select(l => new
                {
                    Status = l.LegStatus.ToString(),
                    BranchName = l.Branch != null ? l.Branch.BranchName : "N/A"
                })
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (order is null)
            return "Order not found, or you do not have permission to view this order.";

        return JsonSerializer.Serialize(order, JsonOptions);
    }
}