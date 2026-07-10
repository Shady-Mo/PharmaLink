namespace Domain.Entities;

public class OrderFulfillmentLegStatusAudit
{
    public Guid AuditId { get; set; }

    public Guid LegId { get; set; }

    public Guid ChangedByUserId { get; set; }

    public LegStatus OldStatus { get; set; }

    public LegStatus NewStatus { get; set; }

    public string Reason { get; set; } = string.Empty;

    public DateTime ChangedAtUtc { get; set; }

    public OrderFulfillmentLeg Leg { get; set; } = null!;
    public AppUser ChangedByUser { get; set; } = null!;
}
