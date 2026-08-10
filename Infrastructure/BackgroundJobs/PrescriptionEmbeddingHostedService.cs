using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.BackgroundJobs
{
    public class PrescriptionEmbeddingHostedService : BackgroundService
    {
        private readonly IBackgroundTaskQueue _queue;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PrescriptionEmbeddingHostedService> _logger;

        public PrescriptionEmbeddingHostedService(
            IBackgroundTaskQueue queue,
            IServiceProvider serviceProvider,
            ILogger<PrescriptionEmbeddingHostedService> logger)
        {
            _queue = queue;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var workItem = await _queue.DequeueAsync(stoppingToken);
                using var scope = _serviceProvider.CreateScope();
                try
                {
                    await workItem(scope.ServiceProvider, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Prescription embedding background job failed");
                }
            }
        }
    }
}
