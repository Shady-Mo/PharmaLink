using Application.DTOs.PreparationList.Request;
using Application.DTOs.PreparationList.Response;
using Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PreparationListController(IPreparationListService preparationListService)
        : BaseApiController
    {
        [HttpGet("")]
        [ProducesResponseType(typeof(PaginatedList<PreparationListDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize(Roles = $"{AppRoles.Pharmacist}")]
        public async Task<IActionResult> GetProfile(
            [FromQuery] PreparationListQueryParameters parameters,
            CancellationToken cancellationToken)
        {
            var id = User.FindFirst(JwtClaimTypes.UserId)?.Value;
            Guid.TryParse(id, out Guid userId);

            var result = await preparationListService.GetPreparationListByPharmacistId(userId, parameters);

            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }
    }
}
