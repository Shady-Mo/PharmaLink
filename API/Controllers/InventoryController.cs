namespace API.Controllers;

public class InventoryController(IInventoryService inventoryService, IWebHostEnvironment env) : BaseApiController
{
    [Authorize(Roles = $"{AppRoles.Pharmacist}")]
    [HttpPost("")]
    [ProducesResponseType(typeof(PharmacyInventoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateDrug(AddPharmacyInventoryDto dto, CancellationToken cancellationToken)
    {
        var result = await inventoryService.CreateAsync(dto, cancellationToken);

        return result.IsSuccess
            ? Created("", result.Value)
            : result.ToProblem();
    }

    [Authorize(Roles = $"{AppRoles.Pharmacist}")]
    [HttpPut("")]
    [ProducesResponseType(typeof(PharmacyInventoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateDrug(UpdatePharmacyInventoryDto dto, CancellationToken cancellationToken)
    {
        var result = await inventoryService.UpdateAsync(dto, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Pharmacist}")]
    [HttpGet("")]
    [ProducesResponseType(typeof(PaginatedList<PharmacyInventoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetInventory(
        [FromQuery] GetPharmacyInventoryParamRequest parameters,
        CancellationToken cancellationToken)
    {
        var result = await inventoryService.GetInventoryAsync(parameters, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }
}