namespace Domain.Entities;

public class PharmacyInventory {
    public Guid InventoryID { get; set; }
    public Guid BranchID { get; set; }
    public Guid DrugID { get; set; }
    public int StockQuantity { get; set; }
    public int ReservedQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public DateOnly ExpiryDate { get; set; }
    public DateTime LastSyncedAt { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public PharmacyBranch Branch { get; set; } = null!;
    public Drug Drug { get; set; } = null!;
}
