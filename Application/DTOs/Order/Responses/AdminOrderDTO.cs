namespace Application.DTOs.Order.Responses;

/// <summary>
/// Flat admin-view DTO for a single order row in the orders management table.
/// Contains denormalised display fields (patient name, pharmacy, medicine list) fetched
/// via explicit JOINs rather than Mapster projection to avoid breaking patient-facing flows.
/// </summary>
public class AdminOrderDTO
{
    /// <summary>Unique order identifier.</summary>
    public Guid OrderId { get; set; }

    /// <summary>Short human-readable order reference: "ORD-XXXXXXXX".</summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>Full name of the patient who placed the order.</summary>
    public string PatientName { get; set; } = string.Empty;

    /// <summary>
    /// Name of the primary pharmacy (first preparation leg's pharmacy).
    /// Empty string when no legs have been assigned yet.
    /// </summary>
    public string PrimaryPharmacyName { get; set; } = string.Empty;

    /// <summary>Display-friendly list of medicine brand names in this order.</summary>
    public List<string> MedicineNames { get; set; } = new();

    /// <summary>Total monetary amount of the order.</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Current status of the order.</summary>
    public OrderStatus OrderStatus { get; set; }

    /// <summary>Fulfillment mode (Delivery / Pickup).</summary>
    public FulfillmentMode FulfillmentMode { get; set; }

    /// <summary>UTC timestamp when the order was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Total number of distinct medicine items in the order.</summary>
    public int ItemCount { get; set; }
}
