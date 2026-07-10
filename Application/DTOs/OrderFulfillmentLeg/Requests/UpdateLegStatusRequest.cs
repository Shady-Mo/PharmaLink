namespace Application.DTOs.OrderFulfillmentLeg.Requests;

public class UpdateLegStatusRequest
{
    public LegStatus Status { get; set; }

    public string? Reason { get; set; }
}
