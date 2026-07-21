namespace Application.DTOs.Order.Requests;

/// <summary>
/// Request DTO for the admin order export endpoint.
/// Supports the same filtering criteria as <see cref="GetOrdersRequest"/> plus the output format.
/// </summary>
public class ExportOrdersRequest
{
    /// <summary>Patient full name or order number substring (case-insensitive).</summary>
    public string? Search { get; set; }

    /// <summary>Filter by order status. Null exports all statuses.</summary>
    public OrderStatus? Status { get; set; }

    /// <summary>Export only orders created on or after this UTC date.</summary>
    public DateTime? FromDate { get; set; }

    /// <summary>Export only orders created on or before this UTC date.</summary>
    public DateTime? ToDate { get; set; }

    /// <summary>
    /// Output format: <c>xlsx</c> (default) or <c>csv</c>.
    /// </summary>
    public string Format { get; set; } = "xlsx";
}
