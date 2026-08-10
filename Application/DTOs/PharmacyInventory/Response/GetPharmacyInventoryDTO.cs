namespace Application.DTOs.PharmacyInventory.Response;

public class GetPharmacyInventoryDTO
{
    public Guid InventoryId { get; set; }

    public Guid BranchId { get; set; }

    public string BranchName { get; set; } = string.Empty;

    public Guid DrugId { get; set; }

    public string DrugName { get; set; } = string.Empty;

    public string ArabicName { get; set; } = string.Empty;

    public int StockQuantity { get; set; }

    public int ReservedQuantity { get; set; }

    public decimal UnitPrice { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    public InventoryStockStatus StockStatus { get; set; }

    public string StockStatusLabel { get; set; } = string.Empty;
}
