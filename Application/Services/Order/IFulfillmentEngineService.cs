using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services.Order
{
    public interface IFulfillmentEngineService
    {
        Task<Result> ProcessOrderFulfillmentAsync(Guid orderId, CancellationToken cancellationToken = default);
    }
}
