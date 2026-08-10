using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services.AI
{
    public interface IInventoryForecastingBackgroundJob
    {
        Task RunDailyForecastAsync();
    }
}
