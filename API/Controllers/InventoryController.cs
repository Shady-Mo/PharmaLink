using Application.DTOs.Supplier;
using Application.Services.AI;
using Infrastructure.Services;
using Twilio.TwiML.Messaging;

namespace API.Controllers;

public class InventoryController(IInventoryService inventoryService, IInventoryForecastingService _forecastingService, IPurchaseOrderService _poService, IInventoryReportService _reportService, ISupplierOrderService _supplierOrderService) : BaseApiController
{
    /// <summary>
    /// Retrieves a paginated inventory list, with optional text search and stock-status filtering.
    /// </summary>
    [Authorize(Roles = $"{AppRoles.PharmacyAdmin},{AppRoles.Pharmacist}")]
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<GetPharmacyInventoryDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetInventory(
        [FromQuery] GetPharmacyInventoryParamRequest parameters,
        CancellationToken cancellationToken)
    {
        var result = await inventoryService.GetInventoryAsync(parameters, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    /// Retrieves a paginated inventory list for a specific branch, with optional text search and stock-status filtering
    /// </summary>
    [Authorize(Roles = $"{AppRoles.PharmacyAdmin},{AppRoles.Pharmacist}")]
    [HttpGet("branch/{branchId:guid}")]

    [ProducesResponseType(typeof(PaginatedList<GetPharmacyInventoryDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetInventoryByBranch(
        Guid branchId,
        [FromQuery] GetPharmacyInventoryParamRequest parameters,
        CancellationToken cancellationToken)
    {
        parameters.BranchId = branchId;

        var result = await inventoryService.GetInventoryAsync(parameters, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    /// Retrieves a single inventory item by its identifier.
    /// </summary>
    [Authorize(Roles = $"{AppRoles.PharmacyAdmin},{AppRoles.Pharmacist}")]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PharmacyInventoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInventoryById(Guid id, CancellationToken cancellationToken)
    {
        var result = await inventoryService.GetByIdAsync(id, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    /// Adds a new medicine to the pharmacy branch's inventory.
    /// </summary>
    [Authorize(Roles = $"{AppRoles.PharmacyAdmin},{AppRoles.Pharmacist}")]
    [HttpPost]
    [ProducesResponseType(typeof(PharmacyInventoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateInventory([FromBody] AddPharmacyInventoryDto dto,
        CancellationToken cancellationToken)
    {
        var result = await inventoryService.CreateAsync(dto, cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetInventoryById), new { id = result.Value?.InventoryId }, result.Value)
            : result.ToProblem();
    }

    /// <summary>
    /// Updates an inventory item's details or adjusts its stock quantity.
    /// </summary>
    [Authorize(Roles = $"{AppRoles.PharmacyAdmin},{AppRoles.Pharmacist}")]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(PharmacyInventoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateInventory(Guid id, [FromBody] UpdatePharmacyInventoryDto dto,
        CancellationToken cancellationToken)
    {
        var result = await inventoryService.UpdateAsync(id, dto, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    /// Removes an inventory item, subject to business constraints (e.g. no reserved stock).
    /// </summary>
    [Authorize(Roles = $"{AppRoles.PharmacyAdmin},{AppRoles.Pharmacist}")]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteInventory(Guid id, CancellationToken cancellationToken)
    {
        var result = await inventoryService.DeleteAsync(id, cancellationToken);

        return result.IsSuccess ? NoContent() : result.ToProblem();
    }


    [Authorize(Roles = $"{AppRoles.Pharmacist}")]
    [HttpPatch("{id:guid}/adjust-stock")]
    [ProducesResponseType(typeof(PharmacyInventoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AdjustStock(Guid id, [FromBody] AdjustStockDTO dto,
        CancellationToken cancellationToken)
    {

        var result = await inventoryService.AdjustStock(id, dto, cancellationToken);

        return result.IsSuccess ? Ok(new {Message = result.Value }) : result.ToProblem();
    }



    [HttpPost("trigger-forecast")]
    [Authorize(Roles = $"{AppRoles.PharmacyAdmin},{AppRoles.Admin}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> TriggerForecast([FromQuery] Guid? branchId, [FromQuery] int analysisDays = 30)
    {

        var result =  await _forecastingService.RunForecastingCycleAsync(branchId, analysisDays);

       return result.IsSuccess ? Ok(new {
           Success = true,
           Message = branchId.HasValue
                ? $"Forecasting cycle successfully executed for branch: {branchId}"
                : "Forecasting cycle successfully executed for all branches.",
           Timestamp = DateTime.UtcNow
       }) : result.ToProblem();
       
    }


    [HttpGet("branches/{branchId}/forecast-report")]
    [Authorize(Roles = $"{AppRoles.PharmacyAdmin},{AppRoles.Admin}")]
    public async Task<IActionResult> GetForecastReport(Guid branchId, [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 10)
    {
        var (report, totalCount) = await _reportService.GetBranchForecastReportAsync(branchId, pageNumber, pageSize);

        if (report == null)
            return NotFound(new { Success = false, Message = "No forecast data found for this branch." });

        return Ok(new { Success = true, Data = report,
            Pagination = new
            {
                CurrentPage = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            }
        });
    }

  
    [HttpPut("purchase-orders/{orderId}/approve")]
    [Authorize(Roles = $"{AppRoles.PharmacyAdmin},{AppRoles.Admin}")]
    public async Task<IActionResult> ApprovePurchaseOrder(Guid orderId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";

        var result = await _poService.ApprovePurchaseOrderAsync(orderId, userId);

        if (!result)
            return BadRequest(new { Success = false, Message = "Failed to approve. Order might not exist or is already processed." });

        return Ok(new { Success = true, Message = "Purchase order approved successfully." });
    }

    [HttpGet("{branchId}/pending-purchase-orders")]
    [Authorize(Roles = $"{AppRoles.PharmacyAdmin},{AppRoles.Admin}")]
    public async Task<IActionResult> GetPendingPurchaseOrder(Guid branchId)
    {
        var result = await _poService.GetPendingPurchaseOrders(branchId);

        return Ok(result);
    }


    [HttpGet("drugs/{drugId:guid}/suppliers")]
    [Authorize(Roles = $"{AppRoles.PharmacyAdmin}")]
    public async Task<IActionResult> GetSuppliersForDrug(Guid drugId)
    {
        var result = await _supplierOrderService.GetSuppliersForDrugAsync(drugId);

        if (result.IsFailure)
        {
            return result.ToProblem();
        }

        return Ok(result.Value);
    }

    [HttpPost("{orderId:guid}/assign-supplier/{supplierId:guid}")]
    [Authorize(Roles = $"{AppRoles.PharmacyAdmin}")]
    public async Task<IActionResult> AssignSupplierToOrder(Guid orderId, Guid supplierId, Guid branchId)
    {

        var result = await _supplierOrderService.AssignSupplierToOrderAsync(orderId, supplierId, branchId);

        if (result.IsFailure)
        {
            return result.ToProblem();
        }

        return Ok(new { Message = "تم إرسال أمر الشراء للمورد بنجاح." });
    }

    [HttpPost("{orderId:guid}/receive")]
    [Authorize(Roles = $"{AppRoles.PharmacyAdmin}")]
    public async Task<IActionResult> ReceiveOrder(Guid orderId, Guid branchId)
    {

        var result = await _supplierOrderService.ReceiveOrderAsync(orderId, branchId);

        if (result.IsFailure)
        {
            result.ToProblem();
        }

        return Ok(new { Message = "تم استلام الطلبية وتحديث المخزون بنجاح." });
    }

    [HttpGet("supplier-orders/{branchId:guid}")]
    [Authorize(Roles = $"{AppRoles.PharmacyAdmin}")]
    public async Task<IActionResult> GetBranchOrders(Guid branchId, [FromQuery] OrderFilterParams filterParams)
    {
        var result = await _poService.GetBranchOrdersAsync(branchId, filterParams);

        if (result.IsFailure)
            return result.ToProblem();

        return Ok(result.Value);
    }

}
