using Application.Abstractions;
using Application.DTOs.AI;
using Application.Services.AI;
using Application.Services.AI.Models;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.AI.Services;

public sealed class PrescriptionHistoryRagService(
    AppDbContext context,
    IPatientPrescriptionVectorService vectorService,
    IPromptExecutionService promptExecutionService,
    IBackgroundJobClient backgroundJobClient,
    ILogger<PrescriptionHistoryRagService> logger) : IPrescriptionHistoryRagService
{
    private const int MaxSources = 3;
    // Qdrant may contain points from a prior local database. Fetch extra candidates,
    // hydrate them from SQL, then keep only the best valid records.
    private const int CandidateLimit = 20;
    private const double MinimumRelevanceScore = 0.35;

    public async Task<PrescriptionHistoryAnswerResponse> AskAsync(
        Guid patientId,
        string question,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(question))
            throw new ArgumentException("Question is required.", nameof(question));

        var vectorResults = await vectorService.SearchAsync(patientId, question.Trim(), CandidateLimit, cancellationToken);
        var relevantResults = vectorResults
            .Where(result => result.Score >= MinimumRelevanceScore)
            .ToList();

        if (relevantResults.Count == 0)
        {
            return new PrescriptionHistoryAnswerResponse
            {
                Answer = "لم أجد روشتة مطابقة بدرجة كافية في أرشيفك. جرّب ذكر اسم الطبيب أو الدواء أو الفترة الزمنية.",
                Sources = []
            };
        }

        var resultIds = relevantResults.Select(result => result.PrescriptionId).ToList();
        var reviews = await context.PrescriptionReviews
            .AsNoTracking()
            .Include(review => review.Medicines)
            .Where(review => review.PatientUserId == patientId
                && resultIds.Contains(review.PrescriptionReviewId))
            .ToListAsync(cancellationToken);

        var reviewsById = reviews.ToDictionary(review => review.PrescriptionReviewId);
        var sources = relevantResults
            .Where(result => reviewsById.ContainsKey(result.PrescriptionId))
            .Select(result => MapSource(reviewsById[result.PrescriptionId], result.Score))
            .Take(MaxSources)
            .ToList();

        if (sources.Count == 0)
        {
            logger.LogWarning(
                "RAG results for patient {PatientId} could not be hydrated from owned reviews. Candidate prescription IDs: {PrescriptionIds}",
                patientId,
                resultIds);
            return new PrescriptionHistoryAnswerResponse
            {
                Answer = "لم أتمكن من الوصول إلى الروشتات المطابقة في أرشيفك.",
                Sources = []
            };
        }

        var contextJson = JsonSerializer.Serialize(sources);
        var aiResult = await promptExecutionService.ExecuteAsync(new PromptExecutionRequest
        {
            PromptName = "PrescriptionHistoryRag",
            TaskType = AITaskType.Rag,
            Variables = new Dictionary<string, object?>
            {
                ["question"] = question.Trim(),
                ["prescription_context"] = contextJson,
                ["current_date"] = DateTime.UtcNow.ToString("yyyy-MM-dd")
            }
        }, cancellationToken);

        return new PrescriptionHistoryAnswerResponse
        {
            Answer = aiResult.RawResponse,
            Sources = sources
        };
    }

    public async Task<int> QueueReindexAsync(Guid? patientId = null, CancellationToken cancellationToken = default)
    {
        var query = context.PrescriptionReviews.AsNoTracking()
            .Where(review => review.ReviewStatus == PrescriptionReviewStatus.Approved
                || review.ReviewStatus == PrescriptionReviewStatus.OrderCreated);

        if (patientId.HasValue)
            query = query.Where(review => review.PatientUserId == patientId.Value);

        var reviewIds = await query
            .Select(review => review.PrescriptionReviewId)
            .ToListAsync(cancellationToken);

        foreach (var reviewId in reviewIds)
        {
            backgroundJobClient.Enqueue<IPrescriptionEmbeddingJob>(
                job => job.ProcessAsync(reviewId, CancellationToken.None));
        }

        logger.LogInformation("Queued {Count} prescription embeddings for reindexing", reviewIds.Count);
        return reviewIds.Count;
    }

    private static PrescriptionHistorySourceDTO MapSource(PrescriptionReview review, double score) => new()
    {
        PrescriptionId = review.PrescriptionReviewId,
        DoctorName = review.DoctorName,
        Specialty = review.Specialty,
        ClinicOrHospital = review.ClinicOrHospital,
        VisitDate = review.CreatedAt,
        DiagnosisNotes = review.AISummary,
        ImageUrl = review.PrescriptionImagePath,
        RelevanceScore = score,
        Medicines = review.Medicines.Select(medicine => new PrescriptionHistoryMedicineDTO
        {
            PrescriptionReviewMedicineId = medicine.PrescriptionReviewMedicineId,
            MedicineName = medicine.MedicineName,
            Strength = medicine.Strength,
            DosageForm = medicine.DosageForm,
            Dose = medicine.Dose,
            Frequency = medicine.Frequency,
            Quantity = medicine.Quantity,
            MatchedDrugId = medicine.MatchedDrugId,
            SuggestedAlternativeDrugId = medicine.SuggestedAlternativeDrugId,
            CanBeAddedToCart = medicine.MatchedDrugId.HasValue || medicine.SuggestedAlternativeDrugId.HasValue
        }).ToList()
    };
}
