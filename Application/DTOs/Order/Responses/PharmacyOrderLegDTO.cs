namespace Application.DTOs.Order.Responses;

public class PharmacyOrderLegDTO
{
    public Guid LegId { get; set; }

    public Guid BranchId { get; set; }

    public string BranchName { get; set; } = string.Empty;

    public LegType LegType { get; set; }

    public LegStatus LegStatus { get; set; }

    public DateTime ReadyByEstimate { get; set; }

    public DateTime? CompletedAt { get; set; }
}