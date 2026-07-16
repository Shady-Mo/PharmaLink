namespace Application.DTOs.Order.Responses
{
    public class OrderSummaryDTO
    {
        public int TotalBranches { get; set; }
        public int FulfilledItems { get; set; }
        public int PendingItems { get; set; }
        public DateTime? EstimatedReadyAt { get; set; }
        public int? EstimatedPreparationMinutes { get; set; }
    }

    public class GetOrderDTO
    {
        public Guid OrderId { get; set; }
        public Guid DeliveryAddressId { get; set; }

        public FulfillmentMode FulfillmentMode { get; set; }

        public OrderStatus OrderStatus { get; set; }
        public decimal TotalAmount { get; set; }

        public OrderSummaryDTO Summary { get; set; } = null!;

        public ICollection<OrderFulfillmentLegResponseDTO> FulfillmentLegs { get; set; } 
            = new HashSet<OrderFulfillmentLegResponseDTO>();

        public ICollection<OrderItemResponseDTO> PendingAssignmentItems { get; set; } 
            = new HashSet<OrderItemResponseDTO>();
    }
}
