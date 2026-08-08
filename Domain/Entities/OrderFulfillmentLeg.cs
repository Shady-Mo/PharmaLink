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

    /// <summary>
    /// Real-world OSRM driving distance (km) from the patient's delivery location to this branch,
    /// captured from the AI fulfillment plan at split time. Persisted so the order response returns
    /// the SAME distance the routing/preview engine computed (instead of a straight-line estimate).
    /// </summary>
    public double? DistanceKm { get; set; }


    public Order Order { get; set; } = null!;
    public PharmacyBranch Branch { get; set; } = null!;
    public ICollection<OrderFulfillmentLegStatusAudit> StatusAudits { get; set; } = new HashSet<OrderFulfillmentLegStatusAudit>();
}
