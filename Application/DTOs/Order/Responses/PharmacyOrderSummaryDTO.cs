namespace Application.DTOs.Order.Responses;

public class PharmacyOrderSummaryDTO
{
    public Guid OrderId { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public string PatientName { get; set; } = string.Empty;

    public DateTime OrderDate { get; set; }

    public DateTime? DeliveryDate { get; set; }

    public decimal TotalAmount { get; set; }

    public LegStatus LegStatus { get; set; }

    public FulfillmentMode FulfillmentMode { get; set; }

    public int ItemsCount { get; set; }
}
