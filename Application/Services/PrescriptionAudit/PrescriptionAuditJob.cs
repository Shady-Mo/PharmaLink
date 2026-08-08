namespace Application.Services.PrescriptionAudit;

public record PrescriptionAuditJob(
    Guid PrescriptionReviewId,
    Guid PatientUserId,
    string AbsoluteFilePath,
    string RelativeFilePath,
    string OriginalFileName);
