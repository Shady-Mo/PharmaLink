namespace Application.DTOs.Dashboard.Responses;

/// <summary>
/// Recent order summary displayed in the patient dashboard.
/// </summary>
public class RecentOrderSummaryDTO
{
    /// <summary>
    /// Unique identifier of the order.
    /// </summary>
    public Guid OrderId { get; set; }

    /// <summary>
    /// Order number or reference (can be formatted order ID).
    /// </summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// Date when the order was created.
    /// </summary>
    public DateTime OrderDate { get; set; }

    /// <summary>
    /// List of medicines included in this order.
    /// </summary>
    public ICollection<OrderedMedicineDTO> Medicines { get; set; } 
        = new HashSet<OrderedMedicineDTO>();

    /// <summary>
    /// Total amount for this order.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Current status of the order.
    /// </summary>
    public OrderStatus Status { get; set; }
}
