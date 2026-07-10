using Application.Services.FulfillmentLeg;

namespace Infrastructure.Services.FulfillmentLeg
{
    public class FulfillmentLegService(AppDbContext context) : IFulfillmentLegService
    {
        public async Task<Result<bool>> GenerateLegsAsync(Guid orderId)
        {
            var order = await context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order is null)
                return Result.Failure<bool>(OrderErrors.OrderNotFound);

            var assignedItems = order.Items
                .Where(i => i.BranchId != Guid.Empty && i.BranchId != null)
                .ToList();

            if (assignedItems.Count == 0)
            {
                order.OrderStatus = OrderStatus.Cancelled;
                await context.SaveChangesAsync();
                return Result.Success(false);
            }

            var distinctBranchIds = assignedItems
                .Select(i => i.BranchId)
                .Distinct()
                .ToList();

            var calculatedLegType = order.FulfillmentMode == FulfillmentMode.Delivery
                ? LegType.Delivery
                : LegType.Preparation;

            var readyByEstimateTime = DateTime.UtcNow.AddMinutes(30);

            foreach (var branchId in distinctBranchIds)
            {
                var leg = new OrderFulfillmentLeg
                {
                    LegId = Guid.NewGuid(),
                    OrderId = order.OrderId,
                    BranchId = branchId.Value,
                    LegStatus = LegStatus.Pending,
                    LegType = calculatedLegType,
                    ReadyByEstimate = readyByEstimateTime
                };

                context.OrderFulfillmentLegs.Add(leg);
            }

            await context.SaveChangesAsync();

            return Result.Success(true);
        }
    }
}
