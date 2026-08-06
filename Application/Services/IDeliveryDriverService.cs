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
    }
}
