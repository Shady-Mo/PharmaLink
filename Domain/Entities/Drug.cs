namespace Domain.Entities;

public class Drug
{
    public Guid DrugId { get; set; }
    public string GenericName { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; } = string.Empty;
    public string DrugBankId { get; set; } = string.Empty;
    public string RxNormCui { get; set; } = string.Empty;
    public string NdcCode { get; set; } = string.Empty;
    public string Strength { get; set; } = string.Empty;
    public string Form { get; set; } = string.Empty;
    
    public decimal Price { get; set; }
    public string Manufacturer { get; set; } = string.Empty;
    public string ArabicName { get; set; } = string.Empty;
    public string DrugClass { get; set; } = string.Empty;

    public DrugCategory Category { get; set; } = DrugCategory.Other;
    public bool RequiresPrescription { get; set; }
    public bool IsActive { get; set; }

    public ICollection<PharmacyInventory> Inventories { get; set; } = new HashSet<PharmacyInventory>();
    public ICollection<OrderItem> OrderItems { get; set; } = new HashSet<OrderItem>();
    public ICollection<CartItem> CartItems { get; set; } = new HashSet<CartItem>();
    public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();

}