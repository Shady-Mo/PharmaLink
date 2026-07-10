namespace Application.DTOs.Order.Responses
{
    public class GetOrderDTO
    {
        public Guid OrderId { get; set; }
        public Guid DeliveryAddressId { get; set; }

        public FulfillmentMode FulfillmentMode { get; set; }

        public OrderStatus OrderStatus { get; set; }
        public decimal TotalAmount { get; set; }

        public ICollection<OrderItemResponseDTO> Items { get; set; } 
            = new HashSet<OrderItemResponseDTO>();
    }
}
