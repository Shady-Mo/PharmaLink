using Application.Services;
using Domain.Constants;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DrugController : ControllerBase
    {
        private readonly IDrugService drugService;
        private readonly IWebHostEnvironment env;

        public DrugController(IDrugService drugService, IWebHostEnvironment env)
        {
            this.drugService = drugService;
            this.env = env;
        }

        [HttpPost("seed")]
        [Authorize(Roles =AppRoles.Admin)]
        public async Task<IActionResult> SeedCatalog(CancellationToken cancellationToken)
        {
            var filePath = Path.Combine(env.WebRootPath, "Data", "drugs_seed.json");

            await drugService.SeedDrugsAsync(filePath, cancellationToken);

            return Ok(new { message = "Seeding process completed. Check logs for details." });
        }
    }
}
