using Application.Common;
using Domain.Enums;

namespace Application.Services.FulfillmentLeg;

public interface IFulfillmentLegService
{
    Task<Result<bool>> GenerateLegsAsync(Guid orderId);
    Task<Result> UpdateLegStatusAsync(Guid legId, LegStatus newStatus, List<Guid> pharmacistBranchIds, CancellationToken cancellationToken);
    Task<Result> UpdateLegStatusForAdminAsync(Guid legId, LegStatus newStatus, string? auditReason, CancellationToken cancellationToken);
}
