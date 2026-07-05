namespace Domain.Entities;

public class Drug {
    public Guid DrugID { get; set; }
    public string GenericName { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public string DrugBankID { get; set; } = string.Empty;
    public string RxNormCUI { get; set; } = string.Empty;
    public string NdcCode { get; set; } = string.Empty;
    public string Strength { get; set; } = string.Empty;
    public string Form { get; set; } = string.Empty;
    public bool RequiresPrescription { get; set; }
    public bool IsActive { get; set; }

    public ICollection<PharmacyInventory> Inventories { get; set; } = new HashSet<PharmacyInventory>();
    public ICollection<OrderItem> OrderItems { get; set; } = new HashSet<OrderItem>();
}
