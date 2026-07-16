using Application.DTOs.Pharmacy.Request;
using Application.Services.Pharmacy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize(Roles = AppRoles.Admin)]
    public class PharmaciesController(IPharmacyService pharmacyService) : ControllerBase
    {
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPharmacy(Guid id, CancellationToken cancellationToken)
        {
            var result = await pharmacyService.GetPharmacyById(id, cancellationToken);

            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }
        [HttpGet]
        public async Task<IActionResult> GetAllPharmacies([FromQuery] GetPharmaciesRequest request, CancellationToken cancellationToken)
        {
            var result = await pharmacyService.GetAllPharmacies(request, cancellationToken);

            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }
        [HttpPost]
        public async Task<IActionResult> AddPharmacy([FromBody] AddPharmacyDTO addPharmacy, CancellationToken cancellationToken)
        {
            var result = await pharmacyService.AddPharmacy(addPharmacy, cancellationToken);

            return result.IsSuccess
            ? CreatedAtAction(
                actionName: nameof(GetPharmacy),
                routeValues: new { id = result.Value?.PharmacyId },
                value: result.Value
            )
            : result.ToProblem();
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePharmacy(Guid id, [FromBody] UpdatePharmacyDTO UpdatePharmacy, CancellationToken cancellationToken)
        {
            var result = await pharmacyService.UpdatePharmacy(id, UpdatePharmacy, cancellationToken);

            return result.IsSuccess ? NoContent() : result.ToProblem();
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePharmacy(Guid id, CancellationToken cancellationToken)
        {
            var result = await pharmacyService.DeletePharmacy(id, cancellationToken);
            return result.IsSuccess ? NoContent() : result.ToProblem();
        }
    }
}
