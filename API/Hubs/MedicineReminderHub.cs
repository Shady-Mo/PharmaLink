using Microsoft.AspNetCore.SignalR;

namespace API.Hubs;

public class MedicineReminderHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        var patientIdStr = httpContext?.Request.Query["patientId"].ToString();

        if (!string.IsNullOrEmpty(patientIdStr) && Guid.TryParse(patientIdStr, out _))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Patient_{patientIdStr}");
            Console.WriteLine($"[SignalR-Reminders] SUCCESS: Patient {patientIdStr} joined group Patient_{patientIdStr} with ConnectionId {Context.ConnectionId}");
        }
        else
        {
            Console.WriteLine($"[SignalR-Reminders] WARNING: Connection {Context.ConnectionId} did not provide a valid patientId! Provided: '{patientIdStr}'");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Console.WriteLine($"[SignalR-Reminders] DISCONNECT: ConnectionId {Context.ConnectionId} disconnected. Exception: {exception?.Message ?? "None"}");
        await base.OnDisconnectedAsync(exception);
    }
}
