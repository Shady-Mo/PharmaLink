using API.Hubs;
using Application.DTOs.DeliveryDriver;
using Microsoft.AspNetCore.SignalR;

namespace API.Notification
{
    public class DeliveryNotificationService(IHubContext<DeliveryHub> hubContext)
    : IDeliveryNotificationService
    {
        public async Task BroadcastNewDeliveryJobAsync(List<Guid> driverIds, DeliveryJobNotificationDto jobDetails)
        {
            var groupNames = driverIds.Select(id => id.ToString()).ToList();

            await hubContext.Clients.Groups(groupNames).SendAsync("NewDeliveryJob", jobDetails);
        }

        public async Task BroadcastJobClaimedAsync(Guid jobId)
        {
            await hubContext.Clients.All.SendAsync("RemoveDeliveryJob", jobId);
        }

        public async Task NotifyPatientOrderOutForDeliveryAsync(Guid patientUserId, Guid orderId, string driverName)
        {
            var message = $"الطيار {driverName} استلم طلبك وهو في الطريق إليك.";

            await hubContext.Clients.User(patientUserId.ToString()).SendAsync("OrderStatusUpdated", new
            {
                OrderId = orderId,
                Status = "OutForDelivery",
                Message = message
            });
        }

        public async Task NotifyPatientOrderDeliveredAsync(Guid patientUserId, Guid orderId)
        {
            await hubContext.Clients.User(patientUserId.ToString()).SendAsync("OrderStatusUpdated", new
            {
                OrderId = orderId,
                Status = "Completed",
                Message = "تم تسليم طلبك بنجاح. نتمنى لك الشفاء العاجل!"
            });
        }

        public async Task NotifyPharmacyOrderDeliveredAsync(Guid branchId, Guid orderId)
        {
            await hubContext.Clients.Group($"Branch_{branchId}").SendAsync("LegStatusUpdated", new
            {
                OrderId = orderId,
                Status = "Delivered"
            });
        }
    }
}
