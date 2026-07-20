namespace API.Controllers;

[Route("api/v1/pharmacy/dashboard")]
[Authorize(Roles = AppRoles.PharmacyAdmin)]
public class PharmacyDashboardController(IPharmacyDashboardService dashboardService) : BaseApiController
{
    /// <summary>
    /// Retrieves the complete pharmacy owner dashboard for the authenticated owner's pharmacy.
    /// </summary>
    /// <remarks>
    /// **Security guarantees:**
    /// - Restricted to the Pharmacy Owner (PharmacyAdmin) role; all other roles receive 403 Forbidden.
    /// - The PharmacyID is read exclusively from the JWT claims — never from the request — so metrics
    ///   are always scoped to the pharmacy the caller actually owns.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// **200 OK** with the aggregated <see cref="PharmacyDashboardDTO"/>.  
    /// **401 Unauthorized** if the caller is not authenticated.  
    /// **403 Forbidden** if the caller is not a Pharmacy Owner or has no pharmacy context.  
    /// **404 Not Found** if the scoped pharmacy no longer exists.  
    /// **500 Internal Server Error** on unexpected failures.
    /// </returns>
    [HttpGet]
    [ProducesResponseType(typeof(PharmacyDashboardDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPharmacyDashboard(
        CancellationToken cancellationToken = default)
    {
        var pharmacyId = User.GetPharmacyId();
        if (pharmacyId is null)
            return Result.Failure(DashboardErrors.PharmacyContextMissing).ToProblem();

        int lowStockThreshold = 10;
        int recentOrdersCount = 5;

        var result = await dashboardService.GetPharmacyDashboardAsync(
            pharmacyId.Value,
            lowStockThreshold,
            recentOrdersCount,
            cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    /// <summary>
    /// Retrieves the specific branch owner dashboard for the authenticated owner's branch.
    /// </summary>
    /// <remarks>
    /// **Security guarantees:**
    /// - Restricted to the branch Owner (PharmacyAdmin) role; all other roles receive 403 Forbidden.
    /// - The PharmacyID is read exclusively from the JWT claims — never from the request — so metrics
    ///   are always scoped to the branch the caller actually owns.
    /// </remarks>
    /// <param name="id">Id of the branch for which to retrieve the dashboard.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// **200 OK** with the aggregated <see cref="PharmacyDashboardDTO"/>.  
    /// **401 Unauthorized** if the caller is not authenticated.  
    /// **403 Forbidden** if the caller is not a branch Owner or has no branch context.  
    /// **404 Not Found** if the scoped branch no longer exists.  
    /// **500 Internal Server Error** on unexpected failures.
    /// </returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PharmacyDashboardDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetBranchDashboard(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var pharmacyId = User.GetPharmacyId();
        if (pharmacyId is null)
            return Result.Failure(DashboardErrors.PharmacyContextMissing).ToProblem();

        var branchId = User.GetBranchId(id);
        if (branchId is null)
            return Result.Failure(DashboardErrors.BranchContextMissing).ToProblem();

        int lowStockThreshold = 10;
        int recentOrdersCount = 5;

        var result = await dashboardService.GetBranchDashboardAsync(
            id,
            pharmacyId.Value,
            lowStockThreshold,
            recentOrdersCount,
            cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }
}
