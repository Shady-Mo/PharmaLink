using Application.DTOs.PharmacyInventory.Request;
using Application.DTOs.PharmacyInventory.Response;
using Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InventoryController(IInventoryService inventoryService, IWebHostEnvironment env) : BaseApiController
    {

        [HttpPost("")]
        [ProducesResponseType(typeof(PharmacyInventoryDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateDrug(AddPharmacyInventoryDto dto, CancellationToken cancellationToken)
        {
            var result = await inventoryService.CreateAsync(dto, cancellationToken);

            return result.IsSuccess
                ? Created("",result.Value)
                : result.ToProblem();
        }

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
    }
}
