namespace API.Controllers;

public class DrugController(IDrugService drugService, IWebHostEnvironment env) : BaseApiController
{
    [HttpPost("seed")]
    // [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> SeedCatalog(CancellationToken cancellationToken)
    {
        var filePath = Path.Combine(env.WebRootPath, "Data", "drugs_seed.json");

        await drugService.SeedDrugsAsync(filePath, cancellationToken);

        return Ok(new { message = "Seeding process completed. Check logs for details." });
    }
}