using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Abstractions
{
    public interface IBackgroundTaskQueue
    {
        ValueTask QueueAsync(Func<IServiceProvider, CancellationToken, Task> workItem);
        ValueTask<Func<IServiceProvider, CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken);

    }
}
