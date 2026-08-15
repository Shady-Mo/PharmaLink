using Hangfire;

namespace Infrastructure.AI.Services;

public sealed class PrescriptionAnalyticsRagService(
    AppDbContext context,
    IPrescriptionAnalyticsVectorService vectorService,
    IPromptExecutionService promptExecutionService,
    IBackgroundJobClient backgroundJobClient,
    ILogger<PrescriptionAnalyticsRagService> logger) : IPrescriptionAnalyticsRagService
{
    private const int CandidateLimit = 30;
    private const double MinimumRelevanceScore = 0.35;

    public async Task<PrescriptionAnalyticsAnswerResponse> AskAsync(
        string question,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(question))
            throw new ArgumentException("Question is required.", nameof(question));

        var vectorResults = await vectorService.SearchAsync(question.Trim(), CandidateLimit, cancellationToken);
        var relevantResults = vectorResults
            .Where(result => result.Score >= MinimumRelevanceScore)
            .ToList();

        if (relevantResults.Count == 0)
        {
            return new PrescriptionAnalyticsAnswerResponse
            {
                Answer = "لم أجد روشتات تحليليّة مطابقة بدرجة كافية لدعم إجابة هذا السؤال.",
                Sources = []
            };
        }

        var resultIds = relevantResults.Select(result => result.PrescriptionId).ToList();
        var reviews = await context.PrescriptionReviews
            .AsNoTracking()
            .Include(review => review.Medicines)
            .Include(review => review.Patient)
                .ThenInclude(p => p.Addresses)
            .Where(review => resultIds.Contains(review.PrescriptionReviewId))
            .ToListAsync(cancellationToken);

        var reviewsById = reviews.ToDictionary(review => review.PrescriptionReviewId);
        var sources = relevantResults
            .Where(result => reviewsById.ContainsKey(result.PrescriptionId))
            .Select(result => MapSource(reviewsById[result.PrescriptionId], result.Score))
            .ToList();

        if (sources.Count == 0)
        {
            logger.LogWarning(
                "Analytics RAG results could not be hydrated from database. Candidate prescription IDs: {PrescriptionIds}",
                resultIds);
            return new PrescriptionAnalyticsAnswerResponse
            {
                Answer = "لم أتمكن من الوصول إلى الروشتات المطابقة في قاعدة البيانات.",
                Sources = []
            };
        }

        var contextJson = JsonSerializer.Serialize(sources);
        var aiResult = await promptExecutionService.ExecuteAsync(new PromptExecutionRequest
        {
            PromptName = "PrescriptionAnalyticsRag",
            TaskType = AITaskType.Rag,
            Variables = new Dictionary<string, object?>
            {
                ["question"] = question.Trim(),
                ["prescription_context"] = contextJson,
                ["current_date"] = DateTime.UtcNow.ToString("yyyy-MM-dd")
            }
        }, cancellationToken);

        var (answer, filteredIds) = ParseLlmResponse(aiResult.RawResponse);
        logger.LogWarning(aiResult.RawResponse);
        var finalSources = sources;
        if (filteredIds != null)
        {
            finalSources = sources
                .Where(s => filteredIds.Contains(s.PrescriptionId))
                .ToList();
        }

        return new PrescriptionAnalyticsAnswerResponse
        {
            Answer = answer,
            Sources = finalSources
        };
    }

    private static (string Answer, List<Guid>? FilteredIds) ParseLlmResponse(string rawResponse)
    {
        if (string.IsNullOrWhiteSpace(rawResponse))
            return (rawResponse, null);

        try
        {
            var cleaned = rawResponse.Trim();
            if (cleaned.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
            {
                cleaned = cleaned.Substring(7);
                if (cleaned.EndsWith("```"))
                    cleaned = cleaned.Substring(0, cleaned.Length - 3);
            }
            else if (cleaned.StartsWith("```"))
            {
                cleaned = cleaned.Substring(3);
                if (cleaned.EndsWith("```"))
                    cleaned = cleaned.Substring(0, cleaned.Length - 3);
            }

            cleaned = cleaned.Trim();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var parsed = JsonSerializer.Deserialize<PrescriptionAnalyticsLlmOutput>(cleaned, options);

            if (parsed != null && !string.IsNullOrWhiteSpace(parsed.Answer))
            {
                return (parsed.Answer, parsed.RelevantPrescriptionIds);
            }
        }
        catch
        {
            // Ignore parse errors and fallback to raw response
        }

        return (rawResponse, null);
    }

    private class PrescriptionAnalyticsLlmOutput
    {
        [JsonPropertyName("answer")]
        public string? Answer { get; set; }

        [JsonPropertyName("relevant_prescription_ids")]
        public List<Guid>? RelevantPrescriptionIds { get; set; }
    }

    public async Task<int> QueueReindexAsync(CancellationToken cancellationToken = default)
    {
        var reviewIds = await context.PrescriptionReviews.AsNoTracking()
            .Where(review => review.ReviewStatus == PrescriptionReviewStatus.Approved
                || review.ReviewStatus == PrescriptionReviewStatus.OrderCreated)
            .Select(review => review.PrescriptionReviewId)
            .ToListAsync(cancellationToken);

        foreach (var reviewId in reviewIds)
        {
            backgroundJobClient.Enqueue<IPrescriptionEmbeddingJob>(
                job => job.ProcessAsync(reviewId, CancellationToken.None));
        }

        logger.LogInformation("Queued {Count} prescription embeddings for analytics reindexing", reviewIds.Count);
        return reviewIds.Count;
    }

    private static PrescriptionAnalyticsSourceDTO MapSource(PrescriptionReview review, double score) => new()
    {
        PrescriptionId = review.PrescriptionReviewId,
        DoctorName = review.DoctorName,
        Specialty = review.Specialty,
        ClinicOrHospital = review.ClinicOrHospital,
        VisitDate = review.CreatedAt,
        DiagnosisNotes = review.AISummary,
        PatientAddress = FormatAddress(review.Patient?.Addresses),
        ImageUrl = review.PrescriptionImagePath,
        RelevanceScore = score,
        Medicines = review.Medicines.Select(medicine => new PrescriptionAnalyticsMedicineDTO
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

    public static string? FormatAddress(IEnumerable<Address>? addresses)
    {
        if (addresses == null) return null;
        var addressList = addresses.ToList();
        var address = addressList.FirstOrDefault(a => a.IsDefault) ?? addressList.FirstOrDefault();
        if (address == null) return null;

        var parts = new[] { address.AddressLine, address.City, address.Governorate }
            .Where(s => !string.IsNullOrWhiteSpace(s));

        var formatted = string.Join(", ", parts);
        return string.IsNullOrWhiteSpace(formatted) ? null : formatted;
    }
}
