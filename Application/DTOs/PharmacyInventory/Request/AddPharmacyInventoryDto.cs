namespace Application.DTOs.PharmacyInventory.Request;

public class AddPharmacyInventoryDto
{
    public Guid BranchId { get; set; }

    public Guid DrugId { get; set; }

    public int StockQuantity { get; set; }

    public decimal UnitPrice { get; set; }

    public DateOnly? ExpiryDate { get; set; }
}
