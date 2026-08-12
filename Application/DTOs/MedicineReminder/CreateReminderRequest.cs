namespace Application.DTOs.MedicineReminder;

public class CreateReminderRequest
{
    public string MedicineName { get; set; } = string.Empty;
    public string? Dosage { get; set; }
    public string? Notes { get; set; }
    public List<string> ReminderTimes { get; set; } = new(); // ["08:00", "20:00"]
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public Guid? PrescriptionReviewMedicineId { get; set; }
    public bool NotifyByEmail { get; set; } = true;
    public bool NotifyByWhatsApp { get; set; } = false;
}
