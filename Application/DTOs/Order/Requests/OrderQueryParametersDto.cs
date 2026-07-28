namespace Application.DTOs.Order.Requests;

public class OrderQueryParametersDto : PaginatedRequest
{
    public string? Search { get; set; }

    public Guid? BranchId { get; set; }

    public LegStatus? Status { get; set; }

    public DateTime? OrderDateFrom { get; set; }

    public DateTime? OrderDateTo { get; set; }

    public DateTime? DeliveryDateFrom { get; set; }

    public DateTime? DeliveryDateTo { get; set; }

    public PharmacyOrderSort SortBy { get; set; } = PharmacyOrderSort.NewestFirst;
}
