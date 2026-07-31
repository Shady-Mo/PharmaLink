namespace Application.Services.PrescriptionAudit;

public interface IPrescriptionAuditJobQueue
{
    ValueTask EnqueueAsync(
        PrescriptionAuditJob job,
        CancellationToken cancellationToken = default);

    ValueTask<PrescriptionAuditJob> DequeueAsync(
        CancellationToken cancellationToken = default);
}
