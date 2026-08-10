namespace Application.DTOs.PharmacyInventory.Response;

public class PharmacyInventoryDto
{
    public Guid InventoryId { get; set; }

    public Guid BranchId { get; set; }

    public Guid DrugId { get; set; }

    public string BranchName { get; set; } = string.Empty;

    public string DrugName { get; set; } = string.Empty;

    public string GenericName { get; set; } = string.Empty;

    public string ArabicName { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public int StockQuantity { get; set; }

    public int ReservedQuantity { get; set; }

    public int AvailableQuantity { get; set; }

    public decimal UnitPrice { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    public DateTime LastSyncedAt { get; set; }

    public InventoryStockStatus StockStatus { get; set; }

    public string StockStatusLabel { get; set; } = string.Empty;
}
