using Application.DTOs.DeliveryDriver;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services
{
    public interface IDeliveryDriverService
    {
        Task<Result> UpdateLocationAsync(Guid driverId, double longitude, double latitude);

        Task<Result<List<Guid>>> GetNearbyAvailableDriversAsync(Guid branchId);
        Task<Result> AcceptJobAsync(Guid driverId, Guid jobId);
        Task<Result> CompleteJobAsync(Guid driverId, Guid jobId);

        Task<Result> SetStatustToOnline(Guid userId);
        Task<Result> SetStatustToOffline(Guid userId);
        Task<Result<DeliveryJobNotificationDto?>> GetActiveJobAsync(Guid driverId);
        Task<Result<List<DeliveryJobNotificationDto>>> GetAvailableJobsAsync(double? driverLat, double? driverLng);
        Task<Result<PaginatedList<DeliveryJobHistoryDto>>> GetDriverHistoryAsync(Guid driverId, int pageNumber, int pageSize);
    }
}
