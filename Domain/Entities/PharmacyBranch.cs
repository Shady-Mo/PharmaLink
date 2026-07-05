namespace Domain.Entities;

public class PharmacyBranch {
    public Guid BranchID { get; set; }
    public Guid PharmacyID { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Governorate { get; set; } = string.Empty;
    public NetTopologySuite.Geometries.Point? GeoLocation { get; set; }
    public decimal ServiceRadiusKm { get; set; }
    public bool SupportsDelivery { get; set; }
    public bool SupportsPickup { get; set; }

    public Pharmacy Pharmacy { get; set; } = null!;
    public ICollection<PharmacyInventory> Inventories { get; set; } = new HashSet<PharmacyInventory>();
    public ICollection<OrderItem> SuppliedOrderItems { get; set; } = new HashSet<OrderItem>();
    public ICollection<OrderFulfillmentLeg> FulfillmentLegs { get; set; } = new HashSet<OrderFulfillmentLeg>();
}
