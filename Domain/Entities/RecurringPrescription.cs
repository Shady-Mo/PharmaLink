namespace Domain.Entities;

public enum RecurringStatus { Active = 1, Paused = 2, Expired = 3 }

public class RecurringPrescription
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    
    public string Name { get; set; } = string.Empty; // وصف مختصر
    public string? Notes { get; set; }
    
    // The prescription image URL or uploaded prescription reference
    public Guid? PrescriptionId { get; set; } // existing prescription
    
    // Schedule config
    public int IntervalDays { get; set; } // 7, 30, 90, etc.
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public DateOnly NextRunDate { get; set; }
    
    // Fulfillment preferences
    public FulfillmentMode FulfillmentMode { get; set; } = FulfillmentMode.Delivery;
    public Guid? PreferredBranchId { get; set; } // null = AI routing
    public Guid? DeliveryAddressId { get; set; }
    
    // Confirmation settings
    public bool RequireConfirmation { get; set; } = true;
    
    public RecurringStatus Status { get; set; } = RecurringStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public Patient Patient { get; set; } = null!;
    public PharmacyBranch? PreferredBranch { get; set; }
    public Prescription? Prescription { get; set; }
    public ICollection<RecurringPrescriptionRun> Runs { get; set; } = new HashSet<RecurringPrescriptionRun>();
}
