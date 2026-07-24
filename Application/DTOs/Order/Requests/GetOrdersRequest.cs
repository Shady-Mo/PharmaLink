namespace Application.DTOs.Order.Requests;

/// <summary>
/// Paginated request with optional search, filter, and sort criteria for admin order queries.
/// </summary>
public class GetOrdersRequest : PaginatedRequest
{
    /// <summary>Patient full name or order number substring (case-insensitive).</summary>
    public string? Search { get; set; }

    /// <summary>Filter by order status. Null returns all statuses.</summary>
    public OrderStatus? Status { get; set; }

    /// <summary>Filter by fulfillment mode (Delivery / Pickup).</summary>
    public FulfillmentMode? FulfillmentMode { get; set; }

    /// <summary>Filter by fulfillment leg status.</summary>
    public LegStatus? LegStatus { get; set; }

    /// <summary>Include only orders created on or after this UTC date.</summary>
    public DateTime? FromDate { get; set; }

    /// <summary>Include only orders created on or before this UTC date.</summary>
    public DateTime? ToDate { get; set; }

    /// <summary>Sort field: <c>date</c> | <c>amount</c> | <c>status</c>. Default: <c>date</c>.</summary>
    public string SortBy { get; set; } = "date";

    /// <summary>Sort direction: <c>asc</c> | <c>desc</c>. Default: <c>desc</c>.</summary>
    public string SortDir { get; set; } = "desc";
}
