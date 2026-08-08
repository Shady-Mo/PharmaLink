namespace Application.DTOs.Order.Responses;

public class PharmacyOrderItemDTO
{
    public Guid OrderItemId { get; set; }

    public Guid DrugId { get; set; }

    public string DrugName { get; set; } = string.Empty;

    public string ArabicName { get; set; } = string.Empty;

    public string Strength { get; set; } = string.Empty;

    public string Form { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal Subtotal => UnitPrice * Quantity;

    public ItemStatus ItemStatus { get; set; }
}