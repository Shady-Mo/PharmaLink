namespace Application.DTOs.Order.Responses;

public class PharmacyOrderDetailDTO
{
    public Guid OrderId { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public LegStatus LegStatus { get; set; }

    public FulfillmentMode FulfillmentMode { get; set; }

    public DateTime OrderDate { get; set; }

    public DateTime? DeliveryDate { get; set; }

    public bool HasPrescription { get; set; }

    public Guid? PrescriptionId { get; set; }

    public decimal TotalAmount { get; set; }

    public PharmacyOrderPatientDTO Patient { get; set; } = new();

    public PharmacyOrderAddressDTO DeliveryAddress { get; set; } = new();

    public List<PharmacyOrderItemDTO> Items { get; set; } = new();

    public List<PharmacyOrderLegDTO> FulfillmentLegs { get; set; } = new();
}
