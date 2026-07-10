namespace Application.DTOs.Order.Requests
{
    public class OrderItemRequestDTO
    {
        public Guid DrugId { get; set; }
        public int QuantityNeeded { get; set; }
    }
}
