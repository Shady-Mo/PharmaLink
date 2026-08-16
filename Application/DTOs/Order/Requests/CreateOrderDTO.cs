namespace Application.DTOs.Order.Requests
{
    public class CreateOrderDTO
    {
        public Guid DeliveryAddressId { get; set; }

        public FulfillmentMode FulfillmentMode { get; set; }

        public Guid? TemporaryPrescriptionId { get; set; }

        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.CashOnDelivery;

        //public ICollection<OrderItemRequestDTO> Items { get; set; } = new HashSet<OrderItemRequestDTO>();
    }
}
