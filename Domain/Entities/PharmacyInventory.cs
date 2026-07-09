namespace Domain.Entities;

public class PharmacyInventory
{
    public Guid InventoryId { get; set; }
    
    public Guid BranchId { get; set; }
    
    public Guid DrugId { get; set; }

    public int StockQuantity { get; set; }

    public int ReservedQuantity { get; set; }
    public decimal UnitPrice { get; set; }

    public DateOnly ExpiryDate { get; set; }

    public DateTime LastSyncedAt { get; set; }
    public byte[] RowVersion { get; set; } = null!;
    public PharmacyBranch Branch { get; set; } = null!;
    public Drug Drug { get; set; } = null!;
}