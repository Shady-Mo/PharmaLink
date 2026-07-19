namespace Application.DTOs.Dashboard.Responses;

/// <summary>
/// Represents a single step in the order progress timeline.
/// </summary>
public class OrderProgressStepDTO
{
    /// <summary>
    /// Unique identifier of the fulfillment leg.
    /// </summary>
    public Guid FulfillmentLegId { get; set; }

    /// <summary>
    /// Type of the leg (e.g., Preparation, Delivery).
    /// </summary>
    public LegType LegType { get; set; }

    /// <summary>
    /// Current status of the fulfillment leg.
    /// </summary>
    public LegStatus Status { get; set; }

    /// <summary>
    /// Pharmacy branch handling this leg (if applicable).
    /// </summary>
    public string? PharmacyName { get; set; }

    /// <summary>
    /// Estimated or actual completion time for this step.
    /// </summary>
    public DateTime? EstimatedCompletionTime { get; set; }
}
