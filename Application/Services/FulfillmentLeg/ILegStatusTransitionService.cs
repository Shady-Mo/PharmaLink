using Domain.Enums;
using Application.Common;

namespace Application.Services.FulfillmentLeg;

public interface ILegStatusTransitionService
{
    Task<Result> UpdateLegStatusAsync(Guid legId, LegStatus newStatus, List<Guid> pharmacistBranchIds,
        CancellationToken cancellationToken);

    Task<Result> UpdateLegStatusForAdminAsync(Guid legId, LegStatus newStatus, string? auditReason,
        CancellationToken cancellationToken);
}
