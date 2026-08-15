using Application.Abstractions;
using API.Hubs;
using Domain.Entities;
using Microsoft.AspNetCore.SignalR;

namespace API.Services;

public class LiveNotificationService(
    IHubContext<NotificationHub> hubContext,
    ILogger<LiveNotificationService> logger) : ILiveNotificationService
{
    public async Task SendLiveNotificationAsync(Guid userId, AppNotification notification)
    {
        try
        {
            var notificationDto = new
            {
                id = notification.Id,
                title = notification.Title,
                message = notification.Message,
                type = notification.Type,
                url = notification.Url,
                isRead = notification.IsRead,
                createdAt = notification.CreatedAt
            };

            await hubContext.Clients.Group($"User_{userId}").SendAsync("ReceiveNotification", notificationDto);
            logger.LogInformation($"Live notification sent to User_{userId}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error sending live notification to User_{userId}");
        }
    }
}
