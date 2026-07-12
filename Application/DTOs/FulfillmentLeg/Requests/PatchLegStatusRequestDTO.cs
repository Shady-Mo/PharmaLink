using Domain.Enums;

namespace Application.DTOs.FulfillmentLeg.Requests;

public class PatchLegStatusRequestDTO
{
    public LegStatus Status { get; init; }
    public string? AuditReason { get; init; }
}
