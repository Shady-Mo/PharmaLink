namespace Domain.Entities;

public class Order
{
    public Guid OrderId { get; set; }
    
    public Guid PatientUserId { get; set; }
    
    public Guid DeliveryAddressId { get; set; }

    public FulfillmentMode FulfillmentMode { get; set; }

    public OrderStatus OrderStatus { get; set; }
    public decimal TotalAmount { get; set; }

    // Required to correctly determine "recent"/"current" order — do not
    // rely on OrderId (Guid) ordering, it is not chronologically sortable.
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeliveredAt { get; set; }

    public Patient Patient { get; set; } = null!;
    public Address DeliveryAddress { get; set; } = null!;
    public PrescriptionReview? PrescriptionReview { get; set; }
    public ICollection<OrderItem> Items { get; set; } = new HashSet<OrderItem>();
    public ICollection<OrderFulfillmentLeg> FulfillmentLegs { get; set; } = new HashSet<OrderFulfillmentLeg>();
}