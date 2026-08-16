namespace Domain.Entities;

public enum ReminderChannel
{
    Email = 1,
    WhatsApp = 2,
    SMS = 3,
    PushNotification = 4
}

public class MedicineReminderLog
{
    public Guid Id { get; set; }
    public Guid ReminderId { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public ReminderChannel Channel { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }

    public MedicineReminder Reminder { get; set; } = null!;
}