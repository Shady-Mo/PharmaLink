using Application.DTOs.PharmacyBranch.Request;
using Application.DTOs.PharmacyBranch.Response;

namespace Application.Services.Pharmacy;

public interface IPharmacyBranchScheduleService
{
    /// <summary>Returns the full weekly schedule for a branch owned by the given pharmacy.</summary>
    Task<Result<BranchScheduleResponseDto>> GetScheduleAsync(
        Guid pharmacyId,
        Guid branchId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or replaces all schedule rows for the given branch.
    /// Expects exactly 7 entries (one per DayOfWeek).
    /// </summary>
    Task<Result<BranchScheduleResponseDto>> UpsertScheduleAsync(
        Guid pharmacyId,
        Guid branchId,
        UpdateBranchScheduleRequest request,
        CancellationToken cancellationToken = default);
}
