using Application.Abstractions;
using Application.Services.AI.Models;
using Infrastructure.AI.Services;
using Hangfire;

namespace Infrastructure.AI;

public class PrescriptionEmbeddingJob(
    AppDbContext context,
    IPatientPrescriptionVectorService vectorService,
    IPrescriptionAnalyticsVectorService analyticsVectorService,
    ILogger<PrescriptionEmbeddingJob> logger) : IPrescriptionEmbeddingJob
{
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 30, 120, 600 })]
    public async Task ProcessAsync(Guid prescriptionReviewId, CancellationToken cancellationToken = default)
    {
        var review = await context.PrescriptionReviews
            .Include(r => r.Medicines)
            .Include(r => r.Patient)
                .ThenInclude(p => p.Addresses)
            .FirstOrDefaultAsync(r => r.PrescriptionReviewId == prescriptionReviewId, cancellationToken);

        if (review is null)
        {
            logger.LogWarning("PrescriptionReview {Id} not found for embedding job", prescriptionReviewId);
            return;
        }

        review.EmbeddingStatus = PrescriptionEmbeddingStatus.Processing;
        await context.SaveChangesAsync(cancellationToken);

        try
        {
            var drugNames = review.Medicines.Select(m => m.MedicineName).ToList();
            var drugIds = review.Medicines
                .Where(m => m.MatchedDrugId.HasValue)
                .Select(m => m.MatchedDrugId!.Value)
                .ToList();

            var patientAddress = PrescriptionAnalyticsRagService.FormatAddress(review.Patient?.Addresses);

            var record = new PatientPrescriptionEmbeddingRecord
            {
                PrescriptionId = review.PrescriptionReviewId,
                PatientId = review.PatientUserId,
                OrderId = review.CreatedOrderId,
                DoctorName = review.DoctorName,
                Specialty = review.Specialty,
                ClinicOrHospital = review.ClinicOrHospital,
                DiagnosisNotes = review.AISummary,
                VisitDate = review.CreatedAt,
                DrugNames = drugNames,
                DrugIds = drugIds,
                ImageUrl = review.PrescriptionImagePath
            };

            var analyticsRecord = new PrescriptionAnalyticsEmbeddingRecord
            {
                PrescriptionId = review.PrescriptionReviewId,
                OrderId = review.CreatedOrderId,
                DoctorName = review.DoctorName,
                Specialty = review.Specialty,
                ClinicOrHospital = review.ClinicOrHospital,
                DiagnosisNotes = review.AISummary,
                PatientAddress = patientAddress,
                VisitDate = review.CreatedAt,
                DrugNames = drugNames,
                DrugIds = drugIds,
                ImageUrl = review.PrescriptionImagePath
            };

            await vectorService.UpsertPrescriptionAsync(record, cancellationToken);
            await analyticsVectorService.UpsertPrescriptionAsync(analyticsRecord, cancellationToken);

            review.EmbeddingStatus = PrescriptionEmbeddingStatus.Completed;
            review.EmbeddedAt = DateTime.UtcNow;
            review.EmbeddingFailureReason = null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Embedding failed for PrescriptionReview {Id}", prescriptionReviewId);
            review.EmbeddingStatus = PrescriptionEmbeddingStatus.Failed;
            review.EmbeddingFailureReason = ex.Message;
            throw; // let Hangfire retry via [AutomaticRetry]
        }
        finally
        {
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}