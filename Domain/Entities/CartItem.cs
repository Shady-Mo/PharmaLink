namespace Domain.Entities;

public class CartItem
{
    public Guid CartItemId { get; set; }

    public Guid CartId { get; set; }

    public Guid DrugId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPriceSnapshot { get; set; }

    public Cart Cart { get; set; } = null!;

    public Drug Drug { get; set; } = null!;
}
