using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Errors;
using Application.Services.Order;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Order;

public class FulfillmentEngineService(
    AppDbContext context,
    IGeoLookupService geoLookupService,
    ILogger<FulfillmentEngineService> logger) : IFulfillmentEngineService
{
    public async Task<Result> ProcessOrderFulfillmentAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var order = await context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderId == orderId, cancellationToken);

        if (order is null)
        {
            logger.LogWarning("Fulfillment failed: Order {OrderId} not found.", orderId);
            return Result.Failure(FulfillmentErrors.OrderNotFound);
        }

        if (!order.Items.Any())
        {
            logger.LogInformation("Order {OrderId} has no items to fulfill.", orderId);
            return Result.Success();
        }

        // 2. GeoLocation
        var address = await context.Addresses
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AddressId == order.DeliveryAddressId, cancellationToken);

        if (address?.GeoLocation is null)
        {
            logger.LogWarning("Fulfillment failed: Delivery address or GeoLocation missing for Order {OrderId}.", orderId);
            return Result.Failure(FulfillmentErrors.AddressNotFound);
        }

        // 3. (ServiceRadiusKm)
        var nearbyBranches = await geoLookupService.FindNearbyBranchesAsync(
            address.GeoLocation,
            initialRadiusKm: 5.0,
            cancellationToken: cancellationToken);

        //  (Pending Items)
        var pendingItems = order.Items.Where(i => i.ItemStatus == ItemStatus.Pending).ToList();

        foreach (var branchResult in nearbyBranches)
        {
            if (!pendingItems.Any()) break;

            var pendingDrugIds = pendingItems.Select(i => i.DrugId).ToList();
            var branchInventories = await context.PharmacyInventories
                .Where(inv => inv.BranchId == branchResult.BranchID && pendingDrugIds.Contains(inv.DrugId))
                .ToListAsync(cancellationToken);

            var itemsToFulfillAtThisBranch = new List<(OrderItem Item, PharmacyInventory Inventory)>();

            foreach (var item in pendingItems)
            {
                var inventory = branchInventories.FirstOrDefault(inv => inv.DrugId == item.DrugId);

                if (inventory is not null)
                {
                    var availableStock = inventory.StockQuantity - inventory.ReservedQuantity;

                    if (availableStock >= item.QuantityNeeded)
                    {
                        itemsToFulfillAtThisBranch.Add((item, inventory));
                    }
                }
            }

            if (itemsToFulfillAtThisBranch.Any())
            {
                foreach (var (item, inventory) in itemsToFulfillAtThisBranch)
                {
                    inventory.ReservedQuantity += item.QuantityNeeded;

                    item.BranchId = branchResult.BranchID;

                    item.ItemStatus = ItemStatus.Fulfilled;

                    await EnsureFulfillmentLegAsync(order.OrderId, branchResult.BranchID, cancellationToken);

                    logger.LogInformation(
                        "Item {OrderItemId} for Drug {DrugId} successfully assigned to Branch {BranchId}.",
                        item.OrderItemId, item.DrugId, branchResult.BranchID);

                    pendingItems.Remove(item);
                }
            }
        }

        // 5. (Unavailable)
        foreach (var remainingItem in pendingItems)
        {
            remainingItem.ItemStatus = ItemStatus.Unavailable;
            logger.LogWarning("Item {OrderItemId} for Drug {DrugId} marked as Unavailable (No stock within radius bounds).",
                remainingItem.OrderItemId, remainingItem.DrugId);
        }

        // 6. RowVersion
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Automated fulfillment processing completed successfully for Order {OrderId}.", order.OrderId);
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogError(ex, "Concurrency conflict occurred during stock reservation saving for Order {OrderId}.", order.OrderId);
            return Result.Failure(InventoryErrors.ConcurrencyConflict);
        }
    }

    private async Task EnsureFulfillmentLegAsync(Guid orderId, Guid branchId, CancellationToken cancellationToken)
    {
        var legExists = await context.OrderFulfillmentLegs
            .AnyAsync(l => l.OrderId == orderId && l.BranchId == branchId, cancellationToken);

        if (!legExists)
        {
            var newLeg = new OrderFulfillmentLeg
            {
                LegId = Guid.NewGuid(),
                OrderId = orderId,
                BranchId = branchId,
                LegType = LegType.Delivery,  
                LegStatus = LegStatus.Pending 
            };
            context.OrderFulfillmentLegs.Add(newLeg);
        }
    }
}