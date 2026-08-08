using Application.DTOs.DeliveryDriver;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services
{
    public interface IDeliveryNotificationService
    {
        Task BroadcastNewDeliveryJobAsync(List<Guid> driverIds, DeliveryJobNotificationDto jobDetails);

        Task BroadcastJobClaimedAsync(Guid jobId);

        Task NotifyPatientOrderOutForDeliveryAsync(Guid patientUserId, Guid orderId, string driverName);

        Task NotifyPatientOrderDeliveredAsync(Guid patientUserId, Guid orderId);
        Task NotifyPharmacyOrderDeliveredAsync(Guid branchId, Guid orderId);
    }
}
