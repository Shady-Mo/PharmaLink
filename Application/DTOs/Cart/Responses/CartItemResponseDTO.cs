namespace Application.DTOs.Cart.Responses;

public class CartItemResponseDTO
{
    public Guid CartItemId { get; set; }

    public Guid DrugId { get; set; }

    public string DrugBrandName { get; set; } = string.Empty;

    public string DrugGenericName { get; set; } = string.Empty;

    public string DrugArabicName { get; set; } = string.Empty;

    public string? DrugImageUrl { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPriceSnapshot { get; set; }

    public decimal LineTotal => Quantity * UnitPriceSnapshot;
}
