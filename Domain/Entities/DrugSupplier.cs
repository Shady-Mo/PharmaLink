namespace Domain.Entities;

public class DrugSupplier
{
    public int Id { get; set; }
    
    public Guid DrugId { get; set; }
    public Drug Drug { get; set; } = null!;

    public int SupplierId { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public decimal Discount { get; set; }
    public decimal CommercialPrice { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}
