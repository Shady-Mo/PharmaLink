namespace Domain.Entities;

public class MedicineReminder
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    
    // Manual entry
    public string MedicineName { get; set; } = string.Empty;
    public string? Dosage { get; set; }
    public string? Notes { get; set; }
    
    // Times stored as comma-separated "HH:mm" strings (e.g., "08:00,14:00,20:00")
    public string ReminderTimesJson { get; set; } = "[]";
    
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    
    // From prescription (optional)
    public Guid? PrescriptionReviewMedicineId { get; set; }
    
    public bool NotifyByEmail { get; set; } = true;
    public bool NotifyByWhatsApp { get; set; } = false;
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public Patient Patient { get; set; } = null!;
    public ICollection<MedicineReminderLog> Logs { get; set; } = new HashSet<MedicineReminderLog>();
}
