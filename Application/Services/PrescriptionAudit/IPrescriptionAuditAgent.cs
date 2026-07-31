namespace Application.Services.PrescriptionAudit;

public interface IPrescriptionAuditAgent
{
    AgentProfile Profile { get; }

    Task<PrescriptionAuditResult> AuditAsync(
        Guid patientUserId,
        string absoluteFilePath,
        string relativeFilePath,
        string originalFileName,
        CancellationToken cancellationToken = default);

    Task<PrescriptionAuditResult> ProcessExistingReviewAsync(
        Guid prescriptionReviewId,
        string absoluteFilePath,
        string originalFileName,
        CancellationToken cancellationToken = default);
}
