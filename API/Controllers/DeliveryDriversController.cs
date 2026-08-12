using Application.DTOs.DeliveryDriver;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize(Roles = AppRoles.DeliveryDriver)]
    public class DeliveryDriversController(IDeliveryDriverService driverService) : BaseApiController
    {
        [HttpPost("location")]
        public async Task<IActionResult> UpdateLocation([FromBody] UpdateLocationRequest request)
        {
            var driverId = User.GetUserId();

            var result = await driverService.UpdateLocationAsync(driverId, request.Longitude, request.Latitude);

            return result.IsSuccess ? Ok() : result.ToProblem();
        }

        [HttpPost("jobs/{jobId}/accept")]
        public async Task<IActionResult> AcceptJob(Guid jobId)
        {
            var driverId = User.GetUserId();

            var result = await driverService.AcceptJobAsync(driverId, jobId);

            return result.IsSuccess ? Ok(new { Message = "تم قبول الطلب بنجاح" }) : result.ToProblem();
        }

        [HttpPost("jobs/{jobId}/complete")]
        public async Task<IActionResult> CompleteJob(Guid jobId)
        {
            var driverId = User.GetUserId();

            var result = await driverService.CompleteJobAsync(driverId, jobId);

            return result.IsSuccess ? Ok(new { Message = "تم إنهاء الطلب والتسليم بنجاح." }) : result.ToProblem();
        }

        [HttpGet("active-job")]
        public async Task<IActionResult> GetActiveJob()
        {
            var driverId = User.GetUserId();
            var result = await driverService.GetActiveJobAsync(driverId);

            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
        }

        [HttpGet("available-jobs")]
        public async Task<IActionResult> GetAvailableJobs([FromQuery] double? lat, [FromQuery] double? lng)
        {
            var result = await driverService.GetAvailableJobsAsync(lat, lng);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var driverId = User.GetUserId();

            var result = await driverService.GetDriverHistoryAsync(driverId, pageNumber, pageSize);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
        }
    }
}
