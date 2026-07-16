namespace Application.DTOs.Cart.Requests;

public class AddCartItemRequestDTO
{
    public Guid DrugId { get; set; }

    public int Quantity { get; set; }
}
