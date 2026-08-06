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

            double radiusInMeters = (double)(branch.ServiceRadiusKm * 1000);

            var activeTimeThreshold = DateTime.UtcNow.AddMinutes(-15);

            var nearbyDrivers = await context.Set<DeliveryDriver>()
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

            // 🚀 3. إرسال إشعارات الـ SignalR 🚀
            // أ. إشعار للمريض (اختياري بس بيخلي الـ UX حلو)
            var patientUserId = job.FulfillmentLeg.Order.PatientUserId;
            await notificationService.NotifyPatientOrderDeliveredAsync(patientUserId, job.FulfillmentLeg.OrderId);

            // ب. إشعار للصيدلي (عشان الطلب يتشال من شاشته أو حالته تتحدث)
            // لاحظ: الصيدلي في السيستم عندك مربوط بـ BranchId
            await notificationService.NotifyPharmacyOrderDeliveredAsync(job.FulfillmentLeg.BranchId, job.FulfillmentLeg.OrderId);

            return Result.Success();
        }
    }
}
