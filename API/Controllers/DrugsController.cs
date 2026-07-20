namespace API.Controllers;

public class DrugsController(IDrugService drugService, IWebHostEnvironment env) : BaseApiController
{
    /// <summary>
    /// Triggers the database seeding process for Egyptian drugs dataset.
    /// </summary>
    /// <remarks>
    /// This endpoint reads the `egyptian-drugs.json` file and safely seeds the data into the database.
    /// Idempotency is maintained, meaning duplicates are skipped.
    /// </remarks>
    [HttpPost("seed")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> SeedCatalog([FromServices] DrugSeeder seeder, CancellationToken cancellationToken)
    {
        var filePath = Path.Combine(env.WebRootPath, "Data", "egyptian-drugs.json");

        await seeder.SeedAsync(filePath, cancellationToken);

        return Ok(new { message = "Seeding process completed. Check logs for details." });
    }

    /// <summary>
    /// Retrieves a paginated list of drugs from the catalog with optional filtering and sorting.
    /// </summary>
    [HttpGet("")]
    [ProducesResponseType(typeof(PaginatedList<DrugDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Authorize(Roles = $"{AppRoles.Patient},{AppRoles.Pharmacist},{AppRoles.Admin}")]
    public async Task<IActionResult> GetDrugs([FromQuery] DrugSearchRequest filters,
        CancellationToken cancellationToken)
    {
        var result = await drugService.SearchCatalogAsync(filters, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    /// Retrieves a specific drug by its unique identifier.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(DrugDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize(Roles = $"{AppRoles.Pharmacist},{AppRoles.Admin}")]
    public async Task<IActionResult> GetDrugById(Guid id, CancellationToken cancellationToken)
    {
        var result = await drugService.GetByIdAsync(id, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    /// Creates a new drug entry in the catalog.
    /// </summary>
    [HttpPost("")]
    [ProducesResponseType(typeof(DrugDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Authorize(Roles = $"{AppRoles.Pharmacist},{AppRoles.Admin}")]
    public async Task<IActionResult> CreateDrug([FromBody] CreateDrugDto dto, CancellationToken cancellationToken)
    {
        var result = await drugService.CreateAsync(dto, cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetDrugById), new { id = result.Value?.DrugId }, result.Value)
            : result.ToProblem();
    }

    /// <summary>
    /// Updates an existing drug in the catalog.
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(DrugDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Authorize(Roles = $"{AppRoles.Pharmacist},{AppRoles.Admin}")]
    public async Task<IActionResult> UpdateDrug(Guid id, [FromBody] UpdateDrugDto dto,
        CancellationToken cancellationToken)
    {
        var result = await drugService.UpdateAsync(id, dto, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    /// Soft deletes a drug from the catalog.
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize(Roles = $"{AppRoles.Pharmacist},{AppRoles.Admin}")]
    public async Task<IActionResult> DeleteDrug(Guid id, CancellationToken cancellationToken)
    {
        var result = await drugService.DeleteAsync(id, cancellationToken);

        return result.IsSuccess ? NoContent() : result.ToProblem();
    }

    // run for one time this for add category to all drugs that have null category, this is for backfilling the data
    [HttpPost("backfill-categories")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Roles = AppRoles.Patient)]
    public async Task<IActionResult> BackfillCategories(CancellationToken cancellationToken)
    {
        var result = await drugService.BackfillCategoriesAsync(cancellationToken);

        return result.IsSuccess
            ? Ok(new { message = $"Backfilled Category for {result.Value} drug(s)." })
            : result.ToProblem();
    }
}