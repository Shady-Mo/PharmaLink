namespace Application.Services;

public interface IPrescriptionReviewService
{
    Task<Result<PrescriptionReviewUploadResponseDTO>> UploadAndExtractAsync(
        Guid patientUserId,
        UploadPrescriptionDTO dto,
        CancellationToken cancellationToken = default);

    Task<Result<PaginatedList<PrescriptionReviewSummaryDTO>>> GetAllAsync(
    GetPrescriptionReviewsRequest request,
    Guid userId,
    string role);

    Task<Result<PrescriptionReviewDetailDTO>> GetByIdAsync(
        Guid prescriptionReviewId,
        Guid requestingUserId,
        string requestingUserRole);

    Task<Result<PrescriptionReviewDetailDTO>> UpdateMedicinesAsync(
        Guid prescriptionReviewId,
        Guid pharmacistUserId,
        UpdatePrescriptionReviewDTO dto);

    Task<Result> ApproveAsync(
        Guid prescriptionReviewId,
        Guid pharmacistUserId,
        ApproveRejectDTO dto);

    Task<Result> RejectAsync(
        Guid prescriptionReviewId,
        Guid pharmacistUserId,
        ApproveRejectDTO dto);

    Task<Result<CartResponseDTO>> AddMedicinesToCartAsync(
        Guid prescriptionReviewId,
        Guid patientUserId,
        AddPrescriptionReviewMedicinesToCartDTO dto,
        CancellationToken cancellationToken = default);
}
