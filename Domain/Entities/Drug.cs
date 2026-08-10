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
    public decimal FinalPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal CostPrice { get; set; }

    public string Manufacturer { get; set; } = string.Empty;
    public string ArabicName { get; set; } = string.Empty;
    public string DrugClass { get; set; } = string.Empty;

    public string DescriptionEn { get; set; } = string.Empty;
    public string DescriptionAr { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;

    public string BrandEn { get; set; } = string.Empty;
    public string BrandAr { get; set; } = string.Empty;
    public string BrandSlug { get; set; } = string.Empty;
    public string BrandImageUrl { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string FlowType { get; set; } = string.Empty;
    public string FullUrl { get; set; } = string.Empty;

    public string MetaKeywordsEn { get; set; } = string.Empty;
    public string MetaKeywordsAr { get; set; } = string.Empty;
    public string MetaDescriptionEn { get; set; } = string.Empty;
    public string MetaDescriptionAr { get; set; } = string.Empty;
    public string SortingKeywordEn { get; set; } = string.Empty;
    public string SortingKeywordAr { get; set; } = string.Empty;
    public string BundleTagEn { get; set; } = string.Empty;
    public string BundleTagAr { get; set; } = string.Empty;
    public string CouponDescriptionEn { get; set; } = string.Empty;
    public string CouponDescriptionAr { get; set; } = string.Empty;
    public string RawJson { get; set; } = string.Empty;

    public int? CategoryId { get; set; }
    public DrugCategory? Category { get; set; }

    public bool RequiresPrescription { get; set; }
    public bool IsActive { get; set; }

    public bool InStock { get; set; }
    public bool OutOfStock { get; set; }
    public bool LowStock { get; set; }
    public int MaxQuantity { get; set; }
    public int Quantity { get; set; }
    public int PurchaseCount { get; set; }
    public int? GameballPoints { get; set; }
    public int? ChefaaId { get; set; }

    public ICollection<DrugSupplier> Suppliers { get; set; } = new List<DrugSupplier>();
    public ICollection<DrugLandingPage> LandingPages { get; set; } = new List<DrugLandingPage>();

    public ICollection<PharmacyInventory> Inventories { get; set; } = new HashSet<PharmacyInventory>();
    public ICollection<OrderItem> OrderItems { get; set; } = new HashSet<OrderItem>();
    public ICollection<CartItem> CartItems { get; set; } = new HashSet<CartItem>();
    public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
}