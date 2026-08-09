using Application.DTOs.DeliveryDriver;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Services
{
    public class DeliveryDriverService(AppDbContext context, IDeliveryNotificationService notificationService) : IDeliveryDriverService
    {
        public async Task<Result> UpdateLocationAsync(Guid driverId, double longitude, double latitude)
        {
            var driver = await context.Set<DeliveryDriver>()
                                      .FirstOrDefaultAsync(d => d.Id == driverId);

            if (driver is null)
                return Result.Failure(DeliveryDriverErrors.DeliveryNotFound);

            var location = new Point(longitude, latitude) { SRID = 4326 };

            driver.CurrentLocation = location;
            driver.LastLocationUpdateUtc = DateTime.UtcNow;

            await context.SaveChangesAsync();

            return Result.Success();
        }

        public async Task<Result<List<Guid>>> GetNearbyAvailableDriversAsync(Guid branchId)
        {
            var branch = await context.PharmacyBranches
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.BranchId == branchId);

            if (branch is null || branch.GeoLocation is null)
                return Result.Failure<List<Guid>>(PharmacyBranchErrors.BranchNotFound);

            double radiusInMeters = (double)(1000* 1000);

            var activeTimeThreshold = DateTime.UtcNow.AddMinutes(-15);

            var nearbyDrivers = await context.DeliveryDrivers
                .AsNoTracking()
                .Where(d => d.DriverAvailability == DriverStatus.Available)
                .Where(d => d.LastLocationUpdateUtc >= activeTimeThreshold)
                .Where(d => d.CurrentLocation != null)
                .Where(d => d.CurrentLocation!.Distance(branch.GeoLocation) <= radiusInMeters)
                .Select(d => d.Id)
                .ToListAsync();

            return Result.Success(nearbyDrivers);
        }

        public async Task<Result> AcceptJobAsync(Guid driverId, Guid jobId)
        {
            var rowsAffected = await context.DeliveryJobs
                .Where(j => j.JobId == jobId && j.Status == DeliveryJobStatus.Pending)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(j => j.DriverId, driverId)
                    .SetProperty(j => j.Status, DeliveryJobStatus.Accepted)
                    .SetProperty(j => j.AcceptedAtUtc, DateTime.UtcNow));

            if (rowsAffected == 0)
            {
                return Result.Failure(DeliveryDriverErrors.DeliveryPicked);
            }

            await context.SaveChangesAsync();

            var job = await context.DeliveryJobs
                .Include(j => j.FulfillmentLeg)
                    .ThenInclude(l => l.Order)
                .Include(j => j.Driver)
                .FirstOrDefaultAsync(j => j.JobId == jobId);

            if (job != null)
            {
                job.FulfillmentLeg.LegStatus = LegStatus.OutForDelivery;

                if (job.Driver != null)
                    job.Driver.DriverAvailability = DriverStatus.Busy;

                await context.SaveChangesAsync();


                await notificationService.BroadcastJobClaimedAsync(jobId);

                var driverName = job.Driver?.FullName ?? "مندوب التوصيل";
                var patientUserId = job.FulfillmentLeg.Order.PatientUserId;
                var orderId = job.FulfillmentLeg.OrderId;

                await notificationService.NotifyPatientOrderOutForDeliveryAsync(patientUserId, orderId, driverName);
            }

            return Result.Success();
        }

        public async Task<Result> CompleteJobAsync(Guid driverId, Guid jobId)
        {
            var job = await context.Set<DeliveryJob>()
                .Include(j => j.FulfillmentLeg)
                    .ThenInclude(l => l.Order)
                .Include(j => j.Driver)
                .FirstOrDefaultAsync(j => j.JobId == jobId);

            if (job is null)
                return Result.Failure(DeliveryDriverErrors.DeliveryNotFound);


            job.Status = DeliveryJobStatus.Delivered;
            job.CompletedAtUtc = DateTime.UtcNow;

            job.FulfillmentLeg.LegStatus = LegStatus.Delivered;
            job.FulfillmentLeg.CompletedAt = DateTime.UtcNow;

            if (job.Driver != null)
            {
                job.Driver.DriverAvailability = DriverStatus.Available;
            }

            var allLegsDelivered = await context.OrderFulfillmentLegs
                .Where(l => l.OrderId == job.FulfillmentLeg.OrderId)
                .AllAsync(l => l.LegStatus == LegStatus.Delivered || l.LegStatus == LegStatus.Cancelled);

            if (allLegsDelivered)
            {
                job.FulfillmentLeg.Order.OrderStatus = OrderStatus.Completed;
            }

            await context.SaveChangesAsync();

            var patientUserId = job.FulfillmentLeg.Order.PatientUserId;
            await notificationService.NotifyPatientOrderDeliveredAsync(patientUserId, job.FulfillmentLeg.OrderId);

            await notificationService.NotifyPharmacyOrderDeliveredAsync(job.FulfillmentLeg.BranchId, job.FulfillmentLeg.OrderId);

            return Result.Success();
        }

        public async Task<Result> SetStatustToOnline(Guid userId)
        {
            var driver = context.DeliveryDrivers.Find(userId);

            driver.DriverAvailability = DriverStatus.Available;

            context.Update(driver);
            await context.SaveChangesAsync();

            return Result.Success();
        }

        public async Task<Result> SetStatustToOffline(Guid userId)
        {
            var driver = context.DeliveryDrivers.Find(userId);

            driver.DriverAvailability = DriverStatus.Offline;

            context.Update(driver);
            await context.SaveChangesAsync();

            return Result.Success();
        }

        public async Task<Result<List<DeliveryJobNotificationDto>>> GetAvailableJobsAsync(double? driverLat, double? driverLng)
        {
            var pendingJobs = await context.DeliveryJobs
                .Include(j => j.FulfillmentLeg).ThenInclude(l => l.Branch)
                .Include(j => j.FulfillmentLeg).ThenInclude(l => l.Order).ThenInclude(o => o.DeliveryAddress)
                .Where(j => j.Status == DeliveryJobStatus.Pending)
                .ToListAsync();

            var availableJobs = new List<DeliveryJobNotificationDto>();
            double maxRadiusKm = 100.0;

            foreach (var j in pendingJobs)
            {
                var address = j.FulfillmentLeg.Order.DeliveryAddress;
                var branch = j.FulfillmentLeg.Branch;

                if (driverLat.HasValue && driverLng.HasValue && branch.GeoLocation != null)
                {
                    double driverToPharmacyKm = CalculateDistanceKm(
                        driverLat.Value, driverLng.Value,
                        branch.GeoLocation.Y, branch.GeoLocation.X
                    );

                    if (driverToPharmacyKm > maxRadiusKm)
                    {
                        continue;
                    }
                }

                double distanceKm = 0;
                if (branch.GeoLocation != null && address.GeoLocation != null)
                {
                    distanceKm = CalculateDistanceKm(branch.GeoLocation.Y, branch.GeoLocation.X, address.GeoLocation.Y, address.GeoLocation.X);
                    distanceKm = Math.Round(distanceKm, 2);
                }

                availableJobs.Add(new DeliveryJobNotificationDto
                {
                    JobId = j.JobId,
                    PharmacyName = branch.BranchName,
                    FullAddress = $"{address.BuildingNumber} عمارة, دور {address.FloorNumber}, {address.AddressLine}, {address.City}",
                    DeliveryFee = j.DeliveryFee,
                    DistanceKm = distanceKm,
                    Latitude = address.GeoLocation.Y,
                    Longitude = address.GeoLocation.X,
                    PharmacyLatitude = branch.GeoLocation.Y,
                    PharmacyLongitude = branch.GeoLocation.X
                });
            }

            return Result.Success(availableJobs);
        }

        private static double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
        {
            var r = 6371;
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Asin(Math.Min(1, Math.Sqrt(a)));
            return r * c;
        }
        private static double ToRadians(double angle) => Math.PI * angle / 180.0;
    }
}
