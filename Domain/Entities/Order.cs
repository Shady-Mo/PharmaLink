using Domain.Enums;

namespace Domain.Entities;

public class Order {
    public Guid OrderID { get; set; }
    public Guid PatientUserID { get; set; }
    public Guid DeliveryAddressID { get; set; }
    public FulfillmentMode FulfillmentMode { get; set; }
    public OrderStatus OrderStatus { get; set; }
    public decimal TotalAmount { get; set; }

    public Patient Patient { get; set; } = null!;
    public Address DeliveryAddress { get; set; } = null!;
    public ICollection<OrderItem> Items { get; set; } = new HashSet<OrderItem>();
    public ICollection<OrderFulfillmentLeg> FulfillmentLegs { get; set; } = new HashSet<OrderFulfillmentLeg>();
}
