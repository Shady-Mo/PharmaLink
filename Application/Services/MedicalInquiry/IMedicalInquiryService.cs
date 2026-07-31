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
        CancellationToken cancellationToken);

    Task<Result<MedicalInquiryResponse>> AnswerAsync(
        Guid medicalInquiryId,
        Guid answeredByUserId,
        AnswerMedicalInquiryRequest request,
        CancellationToken cancellationToken);
}
