using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Application.Errors;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace API.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/orders")]
[Authorize(Roles = "SystemAdmin")] 
public class AdminOrdersController(
    IFulfillmentEngineService fulfillmentService,
    AppDbContext context,
    ILogger<AdminOrdersController> logger) : ControllerBase
{
    [HttpPost("{orderId}/resplit")]

    /// <summary>
    /// The System Admin role can access to a dedicated support-only to manually re-trigger splitting for a stuck/failed order. This action is restricted exclusively to System Admin and is fully audit-logged (who, when, which order, prior state). .
    /// </summary>
    public async Task<IActionResult> ReSplitOrder(Guid orderId, CancellationToken cancellationToken = default)
    {

        var order = await context.Orders
            .FirstOrDefaultAsync(o => o.OrderId == orderId, cancellationToken);

        if (order is null)
        {
            logger.LogWarning("Admin manual re-split aborted: Order {OrderId} not found.", orderId);
            return NotFound(Result.Failure(FulfillmentErrors.OrderNotFound));
        }

        var adminUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("userId")?.Value
                          ?? "System-Admin-Context";

        logger.LogInformation(
            "AUDIT LOG: System Admin '{AdminId}' re-triggered automated order splitting for Order '{OrderId}' at {Timestamp}. Prior Order Status State was: {PriorStatus}.",
            adminUserId, orderId, DateTime.UtcNow, order.OrderStatus);

        try
        {
            var itemsToReset = await context.OrderItems
                .Where(i => i.OrderId == orderId && (i.ItemStatus == ItemStatus.Unavailable || i.ItemStatus == ItemStatus.Pending))
                .ToListAsync(cancellationToken);

            foreach (var item in itemsToReset)
            {
                item.ItemStatus = ItemStatus.Pending; 
                item.BranchId = null; 
            }

            await context.SaveChangesAsync(cancellationToken);

                // Run the Functionality again -- moshady21
            var fulfillmentResult = await fulfillmentService.ProcessOrderFulfillmentAsync(orderId, cancellationToken);

            if (!fulfillmentResult.IsSuccess)
            {
                logger.LogError("Admin manual re-split execution failed for Order {OrderId} during engine calculation.", orderId);
                return BadRequest(fulfillmentResult);
            }

            logger.LogInformation("Audit Log Success: Order {OrderId} resplit and updated successfully by Admin '{AdminId}'.", orderId, adminUserId);
            return Ok(Result.Success());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Critical failure during manual re-split execution for Order {OrderId} by Admin '{AdminId}'.", orderId, adminUserId);
            return StatusCode(500, Result.Failure(FulfillmentErrors.EngineFailure));
        }
    }
}