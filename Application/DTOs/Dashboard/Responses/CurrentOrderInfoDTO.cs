namespace Application.DTOs.Dashboard.Responses;

/// <summary>
/// Current order information displayed on the patient dashboard.
/// </summary>
public class CurrentOrderInfoDTO
{
    /// <summary>
    /// Unique identifier of the current order.
    /// </summary>
    public Guid OrderId { get; set; }

    /// <summary>
    /// Current status of the order (e.g., Pending, Processing, Shipped, etc.).
    /// </summary>
    public OrderStatus Status { get; set; }

    /// <summary>
    /// Timeline of order fulfillment legs showing preparation and delivery progress.
    /// </summary>
    public ICollection<OrderProgressStepDTO> ProgressTimeline { get; set; } 
        = new HashSet<OrderProgressStepDTO>();
}
