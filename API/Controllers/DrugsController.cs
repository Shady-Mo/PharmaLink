using Application.DTOs.Drug.Requests;

namespace API.Controllers;

public class DrugsController(IDrugService drugService, IWebHostEnvironment env) : BaseApiController
{
    [HttpPost("seed")]
    // [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> SeedCatalog(CancellationToken cancellationToken)
    {
        var filePath = Path.Combine(env.WebRootPath, "Data", "drugs_seed.json");

        await drugService.SeedDrugsAsync(filePath, cancellationToken);

        return Ok(new { message = "Seeding process completed. Check logs for details." });
    }

    [HttpGet("")]
    public async Task<IActionResult> GetDrugs([FromQuery] DrugSearchRequest filters,
        CancellationToken cancellationToken)
    {
        var result = await drugService.SearchCatalogAsync(filters, cancellationToken);
        
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetDrugById(Guid id, CancellationToken cancellationToken)
    {
        var result = await drugService.GetByIdAsync(id, cancellationToken);
        
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPost("")]
    public async Task<IActionResult> CreateDrug([FromBody] CreateDrugDto dto, CancellationToken cancellationToken)
    {
        var result = await drugService.CreateAsync(dto, cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetDrugById), new { id = result.Value?.DrugId }, result.Value)
            : result.ToProblem();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateDrug(Guid id, [FromBody] UpdateDrugDto dto,
        CancellationToken cancellationToken)
    {
        var result = await drugService.UpdateAsync(id, dto, cancellationToken);
        
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDrug(Guid id, CancellationToken cancellationToken)
    {
        var result = await drugService.DeleteAsync(id, cancellationToken);
        
        return result.IsSuccess ? NoContent() : result.ToProblem();
    }
}