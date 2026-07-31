using System.Threading.Channels;

namespace Infrastructure.Services.PrescriptionAudit;

public class PrescriptionAuditJobQueue : IPrescriptionAuditJobQueue
{
    private readonly Channel<PrescriptionAuditJob> _queue =
        Channel.CreateUnbounded<PrescriptionAuditJob>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

    public ValueTask EnqueueAsync(
        PrescriptionAuditJob job,
        CancellationToken cancellationToken = default)
    {
        return _queue.Writer.WriteAsync(job, cancellationToken);
    }

    public ValueTask<PrescriptionAuditJob> DequeueAsync(
        CancellationToken cancellationToken = default)
    {
        return _queue.Reader.ReadAsync(cancellationToken);
    }
}
