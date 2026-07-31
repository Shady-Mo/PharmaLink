namespace Domain.Entities;

public class PharmacyBranch
{
    public Guid BranchId { get; set; }
    public Guid PharmacyId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Governorate { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string WorkingHours { get; set; } = string.Empty;
    
    public Point? GeoLocation { get; set; }
    public decimal ServiceRadiusKm { get; set; }
    
    public bool SupportsDelivery { get; set; }
    
    public bool SupportsPickup { get; set; }

    public Pharmacy Pharmacy { get; set; } = null!;
    public ICollection<PharmacyInventory> Inventories { get; set; } = new HashSet<PharmacyInventory>();
    public ICollection<OrderItem> SuppliedOrderItems { get; set; } = new HashSet<OrderItem>();
    public ICollection<PharmacistAssignment> PharmacistAssignments { get; set; } = new HashSet<PharmacistAssignment>();
    public ICollection<OrderFulfillmentLeg> FulfillmentLegs { get; set; } = new HashSet<OrderFulfillmentLeg>();
    public ICollection<PharmacyBranchSchedule> WorkingSchedule { get; set; } = new HashSet<PharmacyBranchSchedule>();
}