using Microsoft.AspNetCore.SignalR;
using API.Extensions;

namespace API.Hubs;

public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        var userIdStr = httpContext?.Request.Query["userId"].ToString();

        if (!string.IsNullOrEmpty(userIdStr) && Guid.TryParse(userIdStr, out var userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
            Console.WriteLine($"[SignalR-Notifications] SUCCESS: User {userId} joined group User_{userId} with ConnectionId {Context.ConnectionId}");
        }
        else
        {
            Console.WriteLine($"[SignalR-Notifications] WARNING: Connection {Context.ConnectionId} did not provide a valid userId!");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Console.WriteLine($"[SignalR-Notifications] DISCONNECT: ConnectionId {Context.ConnectionId} disconnected.");
        await base.OnDisconnectedAsync(exception);
    }
}
