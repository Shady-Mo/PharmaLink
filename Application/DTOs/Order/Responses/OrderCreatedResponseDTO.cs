namespace Application.DTOs.Order.Responses
{
    public class OrderCreatedResponseDTO
    {
        public Guid OrderId { get; set; }
        public OrderStatus Status { get; set; }
        public string Message { get; set; } = default!;
    }
}
