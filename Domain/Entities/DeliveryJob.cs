namespace Domain.Entities;

public class DeliveryJob
{
    public Guid JobId { get; set; }

    public Guid LegId { get; set; }
    public virtual OrderFulfillmentLeg FulfillmentLeg { get; set; } = null!;

    public Guid? DriverId { get; set; }
    public virtual DeliveryDriver? Driver { get; set; }

    public DeliveryJobStatus Status { get; set; } = DeliveryJobStatus.Pending;
    public decimal DeliveryFee { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? AcceptedAtUtc { get; set; }
    public DateTime? PickedUpAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public enum DeliveryJobStatus
{
    Pending = 1,
    Accepted = 2,
    PickedUp = 3,
    Delivered = 4,
    Cancelled = 5
}

