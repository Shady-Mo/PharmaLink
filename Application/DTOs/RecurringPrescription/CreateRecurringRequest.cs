namespace Application.DTOs.RecurringPrescription;

public class CreateRecurringRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public Guid? PrescriptionId { get; set; }
    public int IntervalDays { get; set; } // 7, 30, 90
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public FulfillmentMode FulfillmentMode { get; set; } = FulfillmentMode.Delivery;
    public Guid? PreferredBranchId { get; set; }
    public Guid? DeliveryAddressId { get; set; }
    public bool RequireConfirmation { get; set; } = true;
}