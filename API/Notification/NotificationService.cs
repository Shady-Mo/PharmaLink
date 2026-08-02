using Application.DTOs.Notification;
using Application.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace API.Notification
{
    public class NotificationService(IEmailService _emailService, IHubContext<InventoryHub> _hubContext, IAuthService authService) : INotificationService
    {

        public async Task SendPoCreatedNotificationAsync(PoNotificationDto notification, string email)
        {
            string groupName = $"Branch_{notification.BranchId}";

            await _hubContext.Clients.Group(groupName).SendAsync("ReceivePoAlert", notification);
            await _hubContext.Clients.All.SendAsync("ReceivePoAlert", notification);

            // 2. إرسال الإيميل (Email Delivery)

            string emailBody = $@"
                <h3>Critical Stock Alert</h3>
                <p><strong>Drug:</strong> {notification.DrugName}</p>
                <p><strong>Current Stock:</strong> {notification.CurrentStock}</p>
                <p><strong>Predicted Stock-out Date:</strong> {notification.PredictedStockoutDate?.ToString("yyyy-MM-dd") ?? "N/A"}</p>
                <p><strong>Recommended Order:</strong> {notification.RecommendedOrderQuantity}</p>
                <p><strong>AI Reasoning:</strong> {notification.AiRationale}</p>
            ";

            await _emailService.SendEmailAsync(email, "AI Purchase Order Generated", emailBody);

        }
    }
}
