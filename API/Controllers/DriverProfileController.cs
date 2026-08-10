using Application.DTOs.DeliveryDriver;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    public class DriverProfileController(IDriverProfileService driverProfileService) : BaseApiController
    {
        [HttpGet("")]
        [ProducesResponseType(typeof(GetDriverProfileResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
        {
            var driverId = User.GetUserId();

            var result = await driverProfileService.GetByIdAsync(driverId, cancellationToken);

            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }

        [HttpPut("")]
        [ProducesResponseType(typeof(UpdateDriverProfileResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(UpdateDriverProfileRequestDTO dto, CancellationToken cancellationToken)
        {
            var driverId = User.GetUserId();

            var result = await driverProfileService.UpdateAsync(driverId, dto, cancellationToken);

            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }
    }
}
