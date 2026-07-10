namespace Application.DTOs.Order.Responses
{
    public class OrderItemResponseDTO
    {
        public Guid OrderItemId { get; set; }
        public Guid DrugId { get; set; }
        public int QuantityNeeded { get; set; }
        public ItemStatus ItemStatus { get; set; }
    }
}
