using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services.AI
{
    public interface IInventoryForecastingService
    {
        Task<Result> RunForecastingCycleAsync(Guid? branchId = null, int analysisDays = 30);
    
    }
}
