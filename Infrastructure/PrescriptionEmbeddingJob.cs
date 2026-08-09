using Hangfire;

namespace Infrastructure.AI;

public class PrescriptionEmbeddingJob(
    AppDbContext context,
    IPatientPrescriptionVectorService vectorService,
    ILogger<PrescriptionEmbeddingJob> logger) : IPrescriptionEmbeddingJob
{
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 30, 120, 600 })]
    public async Task ProcessAsync(Guid prescriptionReviewId, CancellationToken cancellationToken = default)
    {
        var review = await context.PrescriptionReviews
            .Include(r => r.Medicines)
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
                DrugNames = review.Medicines.Select(m => m.MedicineName).ToList(),
                DrugIds = review.Medicines
                    .Where(m => m.MatchedDrugId.HasValue)
                    .Select(m => m.MatchedDrugId!.Value)
                    .ToList(),
                ImageUrl = review.PrescriptionImagePath
            };

            await vectorService.UpsertPrescriptionAsync(record, cancellationToken);

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