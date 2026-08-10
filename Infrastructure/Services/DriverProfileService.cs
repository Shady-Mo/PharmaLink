using Application.DTOs.DeliveryDriver;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Services
{
    public class DriverProfileService(
        AppDbContext context,
        ILogger<DriverProfileService> logger) : IDriverProfileService
    {
        public async Task<Result<GetDriverProfileResponseDTO>> GetByIdAsync(Guid driverId, CancellationToken cancellationToken)
        {
            var driver = await context.Set<DeliveryDriver>()
                .AsNoTracking()
                .Include(d => d.DeliveryJobs)
                .Where(d => d.Id == driverId)
                .Select(d => new GetDriverProfileResponseDTO
                {
                    DriverId = d.Id,
                    Email = d.Email,
                    FullName = d.FullName,
                    PhoneNumber = d.PhoneNumber,
                    VehicleInfo = d.VehicleInfo,
                    LastLocationUpdateUtc = d.LastLocationUpdateUtc,
                    TotalCompletedJobs = d.DeliveryJobs.Count(j => j.Status == DeliveryJobStatus.Delivered)
                })
                .FirstOrDefaultAsync(cancellationToken);


            return Result.Success(driver);
        }

        public async Task<Result<UpdateDriverProfileResponseDTO>> UpdateAsync(Guid driverId, UpdateDriverProfileRequestDTO request, CancellationToken cancellationToken)
        {
            var existingByPhone = await context.AppUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber && u.Id != driverId, cancellationToken);

            if (existingByPhone is not null)
            {
                return Result.Failure<UpdateDriverProfileResponseDTO>(PharmacistErrors.PhoneAlreadyExists);
            }

            var driver = await context.Set<DeliveryDriver>()
                .FirstOrDefaultAsync(d => d.Id == driverId, cancellationToken);

            driver.FullName = request.FullName;
            driver.PhoneNumber = request.PhoneNumber;
            driver.VehicleInfo = request.VehicleInfo;

            context.Update(driver);
            await context.SaveChangesAsync(cancellationToken);

            var response = new UpdateDriverProfileResponseDTO
            {
                DriverId = driver.Id,
                FullName = driver.FullName,
                PhoneNumber = driver.PhoneNumber,
                VehicleInfo = driver.VehicleInfo
            };

            return Result.Success(response);
        }
    }
}
