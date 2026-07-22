namespace Application.DTOs.Order.Responses;

/// <summary>
/// Full order detail for the admin order detail page.
/// Combines order info, patient info, fulfillment legs, and all items.
/// </summary>
public class AdminOrderDetailDTO
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string PatientEmail { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public OrderStatus OrderStatus { get; set; }
    public FulfillmentMode FulfillmentMode { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }

    /// <summary>Delivery address summary.</summary>
    public string DeliveryAddress { get; set; } = string.Empty;

    /// <summary>All items in the order with drug details.</summary>
    public List<AdminOrderItemDTO> Items { get; set; } = new();

    /// <summary>Fulfillment legs with pharmacy and status details.</summary>
    public List<AdminOrderLegDTO> FulfillmentLegs { get; set; } = new();
}

public class AdminOrderItemDTO
{
    public Guid OrderItemId { get; set; }
    public Guid DrugId { get; set; }
    public string DrugName { get; set; } = string.Empty;
    public string? GenericName { get; set; }
    public string Strength { get; set; } = string.Empty;
    public string DosageForm { get; set; } = string.Empty;
    public int QuantityNeeded { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal => UnitPrice * QuantityNeeded;
    public ItemStatus ItemStatus { get; set; }
}

public class AdminOrderLegDTO
{
    public Guid LegId { get; set; }
    public LegType LegType { get; set; }
    public LegStatus LegStatus { get; set; }
    public string PharmacyName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public DateTime ReadyByEstimate { get; set; }
    public List<string> MedicineNames { get; set; } = new();
}
