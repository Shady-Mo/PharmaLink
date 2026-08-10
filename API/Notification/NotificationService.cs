using Application.DTOs.Notification;
using Application.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace API.Notification
{
    public class NotificationService(IEmailService _emailService, IHubContext<InventoryHub> _hubContext) : INotificationService
    {

        public async Task SendPoCreatedNotificationAsync(PoNotificationDto notification, string email)
        {
            string groupName = $"Branch_{notification.BranchId}";

            await _hubContext.Clients.Group(groupName).SendAsync("ReceivePoAlert", notification);


            string emailBody = $@"
                <h3>Critical Stock Alert</h3>
                <p><strong>Branch</strong> {notification.BranchName}</p>
            ";

            await _emailService.SendEmailAsync(email, "AI Purchase Order Generated", emailBody);

        }
    }
}
