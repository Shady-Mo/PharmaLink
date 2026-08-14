using API.Hubs;
using Application.Services;
using Application.Abstractions;
using Microsoft.AspNetCore.SignalR;

namespace API.Services;

public class SignalRReminderPushService(
    IHubContext<MedicineReminderHub> hub,
    ILogger<SignalRReminderPushService> logger,
    IWebPushNotificationService webPushService) : IReminderPushService
{
    public async Task PushReminderAsync(Guid patientId, string medicineName, string? dosage, string? notes, string time)
    {
        logger.LogInformation("[SignalRReminderPushService] Pushing reminder to Patient_{PatientId}: {MedicineName} at {Time}", patientId, medicineName, time);
        
        // Push via SignalR (if user is currently active/online on the web app)
        await hub.Clients
            .Group($"Patient_{patientId}")
            .SendAsync("ReceiveMedicineReminder", new
            {
                medicineName,
                dosage,
                notes,
                time
            });

        // Push via WebPush (background notification for mobile devices and offline users)
        string title = "⏰ تذكير بموعد الدواء";
        string message = $"حان موعد تناول دواء {medicineName} {(string.IsNullOrWhiteSpace(dosage) ? "" : $"(الجرعة: {dosage})")}. نتمنى لك الشفاء العاجل!";
        
        try 
        {
            await webPushService.SendNotificationAsync(patientId, title, message, "/patient/medicines");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send WebPush notification for reminder to patient {PatientId}", patientId);
        }
    }
}
