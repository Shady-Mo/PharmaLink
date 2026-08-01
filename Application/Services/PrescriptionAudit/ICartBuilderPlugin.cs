namespace Application.Services.PrescriptionAudit;

public interface ICartBuilderPlugin
{
    Task<CartBuildResult> CreateCartAsync(
        Guid patientUserId,
        Guid prescriptionReviewId,
        IReadOnlyCollection<PrescriptionReviewMedicine> medicines,
        CancellationToken cancellationToken = default);
}
