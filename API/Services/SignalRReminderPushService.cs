using API.Hubs;
using Application.Abstractions;
using Microsoft.AspNetCore.SignalR;

namespace API.Services;

public class SignalRReminderPushService(
    IHubContext<MedicineReminderHub> hub,
    ILogger<SignalRReminderPushService> logger,
    IWebPushNotificationService webPushService) : IReminderPushService
{
    public async Task PushReminderAsync(Guid patientId, string medicineName, string? dosage, string? notes, string time,
        Guid? logId = null)
    {
        logger.LogInformation(
            "[SignalRReminderPushService] Pushing reminder to Patient_{PatientId}: {MedicineName} at {Time}", patientId,
            medicineName, time);

        // Push via SignalR (if user is currently active/online on the web app)
        await hub.Clients
            .Group($"Patient_{patientId}")
            .SendAsync("ReceiveMedicineReminder", new
            {
                medicineName,
                dosage,
                notes,
                time,
                logId
            });

        // Push via WebPush (background notification for mobile devices and offline users)
        string title = "⏰ تذكير بموعد الدواء";
        string message =
            $"حان موعد تناول دواء {medicineName} {(string.IsNullOrWhiteSpace(dosage) ? "" : $"(الجرعة: {dosage})")}. نتمنى لك الشفاء العاجل!";

        try
        {
            var actions = new object[]
            {
                new { action = "take_dose", title = "تم أخذ الجرعة ✅" },
                new { action = "snooze", title = "تأجيل 15 دقيقة ⏰" }
            };

            var onActionClick = new
            {
                @default = new { operation = "navigateLastFocusedOrOpen", url = "/patient/reminders" },
                take_dose = new
                    { operation = "navigateLastFocusedOrOpen", url = $"/patient/reminders?action=take&id={logId}" },
                snooze = new
                    { operation = "navigateLastFocusedOrOpen", url = $"/patient/reminders?action=snooze&id={logId}" }
            };

            await webPushService.SendNotificationAsync(
                userId: patientId,
                title: title,
                message: message,
                url: "/patient/reminders",
                notificationType: "Reminder",
                relatedEntityId: logId,
                tag: "reminders",
                actions: actions,
                onActionClick: onActionClick
            );
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send WebPush notification for reminder to patient {PatientId}", patientId);
        }
    }
}