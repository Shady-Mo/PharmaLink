namespace Application.DTOs.PharmacyInventory.Request;

public class GetPharmacyInventoryParamRequest : PaginatedRequest
{
    public string? Search { get; set; }

    public Guid? BranchId { get; set; }

    public InventoryStatusFilter StatusFilter { get; set; } = InventoryStatusFilter.All;
}
