using Application.Common;
using Application.DTOs.MedicalInquiry.Requests;
using Application.DTOs.MedicalInquiry.Responses;

namespace Application.Services.MedicalInquiry;

public interface IMedicalInquiryService
{
    Task<Result<MedicalInquiryResponse>> CreateAsync(
        Guid patientUserId,
        CreateMedicalInquiryRequest request,
        CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<MedicalInquiryResponse>>> GetMineAsync(
        Guid patientUserId,
        CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<MedicalInquiryResponse>>> GetForReviewTeamAsync(
        string? status,
        CancellationToken cancellationToken);

    Task<Result<MedicalInquiryMetricsResponse>> GetMetricsAsync(CancellationToken cancellationToken);

    Task<Result<MedicalInquiryResponse>> AnswerAsync(
        Guid medicalInquiryId,
        Guid answeredByUserId,
        AnswerMedicalInquiryRequest request,
        CancellationToken cancellationToken);

    Task<Result<MedicalInquiryResponse>> CloseAsync(
        Guid medicalInquiryId,
        CancellationToken cancellationToken);
}
