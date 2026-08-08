using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Domain.Enums;
using Infrastructure.Data;
using System.IO;

namespace Infrastructure.Services
{
    public class PrescriptionCleanupService : BackgroundService
    {
        private readonly ILogger<PrescriptionCleanupService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(12);

        public PrescriptionCleanupService(ILogger<PrescriptionCleanupService> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Prescription Cleanup Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await DoWorkAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during Prescription Cleanup.");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("Prescription Cleanup Service is stopping.");
        }

        private async Task DoWorkAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Prescription Cleanup running at: {time}", DateTimeOffset.Now);

            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Find prescriptions that have been Pending for more than 48 hours
            var expirationThreshold = DateTime.UtcNow.AddHours(-48);

            var expiredPrescriptions = await dbContext.Prescriptions
                .Where(p => p.Status == PrescriptionStatus.Pending && p.UploadedAt < expirationThreshold)
                .ToListAsync(stoppingToken);

            if (expiredPrescriptions.Any())
            {
                _logger.LogInformation("Found {count} expired prescriptions to clean up.", expiredPrescriptions.Count);

                foreach (var prescription in expiredPrescriptions)
                {
                    try
                    {
                        if (File.Exists(prescription.StoragePath))
                        {
                            File.Delete(prescription.StoragePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete file {path}", prescription.StoragePath);
                    }
                }

                dbContext.Prescriptions.RemoveRange(expiredPrescriptions);
                await dbContext.SaveChangesAsync(stoppingToken);

                _logger.LogInformation("Cleaned up {count} expired prescriptions.", expiredPrescriptions.Count);
            }
        }
    }
}
