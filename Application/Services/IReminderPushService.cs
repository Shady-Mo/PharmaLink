namespace Application.Services;

public interface IReminderPushService
{
    Task PushReminderAsync(Guid patientId, string medicineName, string? dosage, string? notes, string time);
}
