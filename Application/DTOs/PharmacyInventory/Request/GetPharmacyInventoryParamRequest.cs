namespace Application.DTOs.PharmacyInventory.Request;

public class GetPharmacyInventoryParamRequest : PaginatedRequest
{
    public string? SerachByName { get; set; }
    public StockStatus? Status { get; set; }
}

public enum StockStatus
{
    LowStock,
    ExpiredSoon
}