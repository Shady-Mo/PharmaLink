using Domain.Enums;

namespace Domain.Entities;

public class OrderItem
{
    public Guid OrderItemID { get; set; }
    public Guid OrderID { get; set; }
    public Guid DrugID { get; set; }
    public Guid BranchID { get; set; }
    public int QuantityNeeded { get; set; }
    public ItemStatus ItemStatus { get; set; }

    public Order Order { get; set; } = null!;
    public Drug Drug { get; set; } = null!;
    public PharmacyBranch Branch { get; set; } = null!;
}
