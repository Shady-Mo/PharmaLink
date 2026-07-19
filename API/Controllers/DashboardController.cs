
using Application.DTOs.Dashboard.Responses;
using Application.Services.Dashboard;

namespace API.Controllers;

/// <summary>
/// Handles patient dashboard operations including statistics, current orders, and recent order history.
/// </summary>
[Authorize(Roles = AppRoles.Patient)]
public class DashboardController(IDashboardService dashboardService) : BaseApiController
{
    /// <summary>
    /// Retrieves the complete patient dashboard with statistics, current order, and recent orders.
    /// </summary>
    /// <remarks>
    /// Provides a quick overview of the patient's account including:
    /// - Dashboard statistics: total orders, pending prescription reviews, saved addresses, reward points
    /// - Current order information with fulfillment progress timeline
    /// - Recent orders for quick access
    /// - Indication of whether more orders are available
    /// 
    /// **Security guarantees:**
    /// - Only authenticated patients can access their own dashboard.
    /// - PatientUserID is derived from the JWT token, not from the request.
    /// </remarks>
    /// <param name="recentOrdersCount">Number of recent orders to retrieve (default: 5, max: 20).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// **200 OK** with the complete PatientDashboardDTO on success.  
    /// **401 Unauthorized** if the user is not authenticated.  
    /// **403 Forbidden** if the user is not a Patient.  
    /// **500 Internal Server Error** if any error occurs during data retrieval.
    /// </returns>
    [HttpGet("")]
    [ProducesResponseType(typeof(PatientDashboardDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDashboard(
        [FromQuery] int recentOrdersCount = 5,
        CancellationToken cancellationToken = default)
    {
        // Validate recentOrdersCount
        if (recentOrdersCount < 1 || recentOrdersCount > 20)
            recentOrdersCount = 5;

        var result = await dashboardService.GetDashboardAsync(
            User.GetUserId(),
            recentOrdersCount,
            cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }
}
