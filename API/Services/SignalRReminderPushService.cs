using API.Hubs;
using Application.Services;
using Microsoft.AspNetCore.SignalR;

namespace API.Services;

public class SignalRReminderPushService(IHubContext<MedicineReminderHub> hub, ILogger<SignalRReminderPushService> logger) : IReminderPushService
{
    public async Task PushReminderAsync(Guid patientId, string medicineName, string? dosage, string? notes, string time)
    {
        logger.LogInformation("[SignalRReminderPushService] Pushing reminder to Patient_{PatientId}: {MedicineName} at {Time}", patientId, medicineName, time);
        await hub.Clients
            .Group($"Patient_{patientId}")
            .SendAsync("ReceiveMedicineReminder", new
            {
                medicineName,
                dosage,
                notes,
                time
            });
    }
}
