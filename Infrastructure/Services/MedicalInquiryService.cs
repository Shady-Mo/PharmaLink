using Application.Common;
using Application.DTOs.MedicalInquiry.Requests;
using Application.DTOs.MedicalInquiry.Responses;
using Application.Errors;
using Application.Services.MedicalInquiry;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class MedicalInquiryService(AppDbContext context) : IMedicalInquiryService
{
    public async Task<Result<MedicalInquiryResponse>> CreateAsync(
        Guid patientUserId,
        CreateMedicalInquiryRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
        {
            return Result.Failure<MedicalInquiryResponse>(MedicalInquiryErrors.EmptyQuestion);
        }

        var inquiry = new MedicalInquiry
        {
            MedicalInquiryId = Guid.NewGuid(),
            PatientUserId = patientUserId,
            Question = request.Question.Trim(),
            Status = MedicalInquiryStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        context.MedicalInquiries.Add(inquiry);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(await MapByIdAsync(inquiry.MedicalInquiryId, cancellationToken));
    }

    public async Task<Result<IReadOnlyList<MedicalInquiryResponse>>> GetMineAsync(
        Guid patientUserId,
        CancellationToken cancellationToken)
    {
        var inquiries = await BaseQuery()
            .Where(i => i.PatientUserId == patientUserId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);

        var items = inquiries.Select(ToResponse).ToList();

        return Result.Success<IReadOnlyList<MedicalInquiryResponse>>(items);
    }

    public async Task<Result<IReadOnlyList<MedicalInquiryResponse>>> GetForReviewTeamAsync(
        CancellationToken cancellationToken)
    {
        var inquiries = await BaseQuery()
            .OrderBy(i => i.Status == MedicalInquiryStatus.Pending ? 0 : 1)
            .ThenByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);

        var items = inquiries.Select(ToResponse).ToList();

        return Result.Success<IReadOnlyList<MedicalInquiryResponse>>(items);
    }

    public async Task<Result<MedicalInquiryResponse>> AnswerAsync(
        Guid medicalInquiryId,
        Guid answeredByUserId,
        AnswerMedicalInquiryRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Answer))
        {
            return Result.Failure<MedicalInquiryResponse>(MedicalInquiryErrors.EmptyAnswer);
        }

        var inquiry = await context.MedicalInquiries
            .FirstOrDefaultAsync(i => i.MedicalInquiryId == medicalInquiryId, cancellationToken);

        if (inquiry is null)
        {
            return Result.Failure<MedicalInquiryResponse>(MedicalInquiryErrors.NotFound);
        }

        if (inquiry.Status != MedicalInquiryStatus.Pending)
        {
            return Result.Failure<MedicalInquiryResponse>(MedicalInquiryErrors.AlreadyAnswered);
        }

        inquiry.Answer = request.Answer.Trim();
        inquiry.AnsweredByUserId = answeredByUserId;
        inquiry.AnsweredAt = DateTime.UtcNow;
        inquiry.Status = MedicalInquiryStatus.Answered;

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(await MapByIdAsync(inquiry.MedicalInquiryId, cancellationToken));
    }

    private IQueryable<MedicalInquiry> BaseQuery() =>
        context.MedicalInquiries
            .AsNoTracking()
            .Include(i => i.Patient)
            .Include(i => i.AnsweredBy);

    private async Task<MedicalInquiryResponse> MapByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var inquiry = await BaseQuery()
            .FirstAsync(i => i.MedicalInquiryId == id, cancellationToken);

        return ToResponse(inquiry);
    }

    private static MedicalInquiryResponse ToResponse(MedicalInquiry inquiry) => new()
    {
        MedicalInquiryId = inquiry.MedicalInquiryId,
        PatientUserId = inquiry.PatientUserId,
        PatientName = inquiry.Patient.FullName,
        Question = inquiry.Question,
        Answer = inquiry.Answer,
        Status = inquiry.Status.ToString(),
        AnsweredByName = inquiry.AnsweredBy?.FullName,
        CreatedAt = inquiry.CreatedAt,
        AnsweredAt = inquiry.AnsweredAt
    };
}
