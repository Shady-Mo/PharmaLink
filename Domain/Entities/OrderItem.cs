namespace Domain.Entities;

public class OrderItem
{
    public Guid OrderItemId { get; set; }
    
    public Guid OrderId { get; set; }
    
    public Guid DrugId { get; set; }
    
    public Guid? BranchId { get; set; }
    
    public int QuantityNeeded { get; set; }
    
    public ItemStatus ItemStatus { get; set; }

    public Order Order { get; set; } = null!;
    public Drug Drug { get; set; } = null!;
    public PharmacyBranch Branch { get; set; } = null!;
}