namespace Application.DTOs.Cart.Responses;

public class CartResponseDTO
{
    public Guid CartId { get; set; }

    public Guid PatientUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public List<CartItemResponseDTO> Items { get; set; } = [];

    public decimal GrandTotal => Items.Sum(i => i.LineTotal);
}
