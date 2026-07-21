namespace API.Controllers;

/// <summary>
/// System Administrator dashboard endpoint.
/// Returns platform-wide statistics, 30-day order analytics, recent orders, and top pharmacies.
/// </summary>
[Route("api/v1/admin/dashboard")]
[ApiController]
[Authorize(Roles = AppRoles.Admin)]
public class AdminDashboardController(IAdminDashboardService adminDashboardService) : ControllerBase
{
    /// <summary>
    /// Retrieves the complete system administrator dashboard.
    /// </summary>
    /// <remarks>
    /// Aggregates the following platform-level data in a single request:
    /// - **Platform Statistics**: total registered patients, verified partner pharmacies,
    ///   all orders, and distinct medicines in the catalog.
    /// - **Order Analytics**: daily order counts for the last 30 days and order status distribution.
    /// - **Recent Orders**: the most recently placed orders with patient name, amount, and status.
    /// - **Top Pharmacies**: top-performing pharmacies ranked by fulfilled order count.
    ///
    /// **Security guarantees:**
    /// - Only authenticated users with the `Admin` role can access this endpoint.
    /// - No patient-identifying data beyond the patient's name is exposed.
    /// </remarks>
    /// <param name="recentOrdersCount">
    /// Number of recent orders to include in the response (default: 10, clamped to 1–50).
    /// </param>
    /// <param name="topPharmaciesCount">
    /// Number of top pharmacies to include (default: 5, clamped to 1–20).
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// **200 OK** with the complete <see cref="AdminDashboardDTO"/> on success.<br/>
    /// **401 Unauthorized** if the user is not authenticated.<br/>
    /// **403 Forbidden** if the user is not an Admin.<br/>
    /// **500 Internal Server Error** if a database error occurs.
    /// </returns>
    [HttpGet("")]
    [ProducesResponseType(typeof(AdminDashboardDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAdminDashboard(
        [FromQuery] int recentOrdersCount = 10,
        [FromQuery] int topPharmaciesCount = 5,
        CancellationToken cancellationToken = default)
    {
        // Clamp parameters to safe ranges
        recentOrdersCount = Math.Clamp(recentOrdersCount, 1, 50);
        topPharmaciesCount = Math.Clamp(topPharmaciesCount, 1, 20);

        var result = await adminDashboardService.GetAdminDashboardAsync(
            recentOrdersCount,
            topPharmaciesCount,
            cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }
}
