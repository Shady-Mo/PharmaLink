namespace Application.DTOs.Drug.Requests;

public class CreateDrugDto
{
    public string GenericName { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public string DrugBankId { get; set; } = string.Empty;
    public string RxNormCui { get; set; } = string.Empty;
    public string NdcCode { get; set; } = string.Empty;
    public string Strength { get; set; } = string.Empty;
    public string Form { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Manufacturer { get; set; } = string.Empty;
    public string ArabicName { get; set; } = string.Empty;
    public string DrugClass { get; set; } = string.Empty;
    public bool RequiresPrescription { get; set; }
    public int? CategoryId { get; set; }
    
    public decimal FinalPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal CostPrice { get; set; }
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
    public bool InStock { get; set; }
    public bool OutOfStock { get; set; }
    public bool LowStock { get; set; }
    public int MaxQuantity { get; set; }
    public int Quantity { get; set; }
    public int PurchaseCount { get; set; }
    public int? GameballPoints { get; set; }
    public int? ChefaaId { get; set; }
}