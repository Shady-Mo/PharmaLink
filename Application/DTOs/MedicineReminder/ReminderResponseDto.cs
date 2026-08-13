namespace Application.DTOs.MedicineReminder;

public class ReminderResponseDto
{
    public Guid Id { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public string? Dosage { get; set; }
    public string? Notes { get; set; }
    public List<string> ReminderTimes { get; set; } = new();
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool NotifyByEmail { get; set; }
    public bool NotifyByWhatsApp { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
