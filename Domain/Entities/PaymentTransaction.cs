namespace Domain.Entities;

public class PaymentTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public string? StripeSessionId { get; set; }
    public string? StripePaymentIntentId { get; set; }

    public decimal Amount { get; set; }
    public string Currency { get; set; } = "egp";

    /// <summary>
    /// e.g. "Created", "Succeeded", "Failed"
    /// </summary>
    public string Status { get; set; } = "Created";

    /// <summary>
    /// e.g. "checkout.session.completed", "payment_intent.payment_failed"
    /// </summary>
    public string? EventType { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Store the raw JSON from Stripe or any error message for debugging purposes
    /// </summary>
    public string? RawData { get; set; }
}