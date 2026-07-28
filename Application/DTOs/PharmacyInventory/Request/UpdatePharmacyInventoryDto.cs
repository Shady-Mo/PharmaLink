namespace Application.DTOs.PharmacyInventory.Request;

public class UpdatePharmacyInventoryDto
{
    public int StockQuantity { get; set; }

    public decimal UnitPrice { get; set; }

    public DateOnly ExpiryDate { get; set; }

    public byte[]? RowVersion { get; set; }
}
