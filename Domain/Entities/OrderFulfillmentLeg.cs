namespace Domain.Entities;

public class OrderFulfillmentLeg
{
    public Guid LegId { get; set; }
    
    public Guid OrderId { get; set; }
    
    public Guid BranchId { get; set; }

    public LegType LegType { get; set; }

    public LegStatus LegStatus { get; set; }

    public DateTime ReadyByEstimate { get; set; }

    public DateTime? CompletedAt { get; set; }

    public Order Order { get; set; } = null!;
    public PharmacyBranch Branch { get; set; } = null!;
}