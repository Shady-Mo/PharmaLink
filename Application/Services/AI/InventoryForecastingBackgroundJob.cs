using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services.AI
{
    public class InventoryForecastingBackgroundJob : IInventoryForecastingBackgroundJob
    {
        private readonly IServiceProvider _serviceProvider;

        public InventoryForecastingBackgroundJob(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task RunDailyForecastAsync()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var forecastingService = scope.ServiceProvider.GetRequiredService<IInventoryForecastingService>();

                await forecastingService.RunForecastingCycleAsync(branchId: null, analysisDays: 30);
            }
        }
    }
}
