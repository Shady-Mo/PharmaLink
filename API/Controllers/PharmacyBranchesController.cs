using Application.DTOs.PharmacyBranch.Request;
using Application.DTOs.PharmacyBranch.Response;

namespace API.Controllers;

[Authorize(Roles = AppRoles.PharmacyAdmin)]
[Route("api/v1/pharmacies/branches")]
[ApiController]
public class PharmacyBranchesController(IPharmacyBranchService branchService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<PharmacyBranchResponseDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(
        [FromQuery] GetPharmacyBranchParamRequest parameters,
        CancellationToken cancellationToken)
    {
        var pharmacyId = User.GetPharmacyId();
        if (pharmacyId is null)
            return Result.Failure(PharmacyBranchErrors.PharmacyContextMissing).ToProblem();

        var result = await branchService.GetAllAsync(pharmacyId.Value, parameters, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PharmacyBranchResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var pharmacyId = User.GetPharmacyId();
        if (pharmacyId is null)
            return Result.Failure(PharmacyBranchErrors.PharmacyContextMissing).ToProblem();

        var result = await branchService.GetByIdAsync(pharmacyId.Value, id, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPost]
    [ProducesResponseType(typeof(PharmacyBranchResponseDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(
        [FromBody] CreatePharmacyBranchDTO dto,
        CancellationToken cancellationToken)
    {
        var pharmacyId = User.GetPharmacyId();
        if (pharmacyId is null)
            return Result.Failure(PharmacyBranchErrors.PharmacyContextMissing).ToProblem();

        var result = await branchService.CreateAsync(pharmacyId.Value, dto, cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value?.BranchId }, result.Value)
            : result.ToProblem();
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(PharmacyBranchResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id,
        [FromBody] UpdatePharmacyBranchDTO dto,
        CancellationToken cancellationToken)
    {
        var pharmacyId = User.GetPharmacyId();
        if (pharmacyId is null)
            return Result.Failure(PharmacyBranchErrors.PharmacyContextMissing).ToProblem();

        var result = await branchService.UpdateAsync(pharmacyId.Value, id, dto, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var pharmacyId = User.GetPharmacyId();
        if (pharmacyId is null)
            return Result.Failure(PharmacyBranchErrors.PharmacyContextMissing).ToProblem();

        var result = await branchService.DeleteAsync(pharmacyId.Value, id, cancellationToken);

        return result.IsSuccess ? NoContent() : result.ToProblem();
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(List<PharmacyBranchSearchDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Search(
        [FromQuery] string? term,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(term))
            return Ok(new List<PharmacyBranchSearchDTO>());

        var pharmacyId = User.GetPharmacyId();
        if (pharmacyId is null)
            return Result.Failure(PharmacyBranchErrors.PharmacyContextMissing).ToProblem();

        var result = await branchService.SearchAsync(pharmacyId.Value, term.Trim(), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }
}
