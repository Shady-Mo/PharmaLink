using Application.Abstractions;
using Application.DTOs.AI;
using Application.Services.AI;
using Application.Services.AI.Models;
using Domain.Entities;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

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

        var contextForLlm = sources.Select(s => new
        {
            shortId = s.PrescriptionId.ToString()[..8],
            doctorName = s.DoctorName,
            specialty = s.Specialty,
            clinicOrHospital = s.ClinicOrHospital,
            visitDate = s.VisitDate,
            diagnosisNotes = s.DiagnosisNotes,
            patientAddress = s.PatientAddress,
            medicines = s.Medicines
        }).ToList();

        var contextJson = JsonSerializer.Serialize(contextForLlm);
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

        var (answer, reasoning, filteredIdStrings, relevantMedicines) = ParseLlmResponse(aiResult.RawResponse);

        if (!string.IsNullOrWhiteSpace(reasoning))
        {
            logger.LogInformation("Analytics RAG Reasoning: {Reasoning}", reasoning);
        }

        var finalSources = sources;
        if (filteredIdStrings != null)
        {
            finalSources = sources
                .Where(s => filteredIdStrings.Any(idStr =>
                    !string.IsNullOrWhiteSpace(idStr) &&
                    (s.PrescriptionId.ToString().StartsWith(idStr.Trim(), StringComparison.OrdinalIgnoreCase) ||
                     idStr.Trim().StartsWith(s.PrescriptionId.ToString()[..8], StringComparison.OrdinalIgnoreCase))))
                .ToList();
        }

        var topPrescribedDrugs = new List<PrescribedDrugMetricDTO>();
        var mostRequestedCategories = new List<CategoryMetricDTO>();

        if (finalSources.Count > 0 && relevantMedicines != null && relevantMedicines.Count > 0)
        {
            var allSourceMedicines = finalSources
                .SelectMany(s => s.Medicines.Select(m => new { Source = s, Medicine = m }))
                .ToList();

            var groupedDrugs = relevantMedicines
                .Where(m => !string.IsNullOrWhiteSpace(m.MedicineName))
                .GroupBy(m => m.MedicineName!.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    var medName = g.Key;
                    var category = g.First().Category?.Trim() ?? "عام";

                    var matchingItems = allSourceMedicines
                        .Where(x => x.Medicine.MedicineName.Equals(medName, StringComparison.OrdinalIgnoreCase) ||
                                    x.Medicine.MedicineName.Contains(medName, StringComparison.OrdinalIgnoreCase) ||
                                    medName.Contains(x.Medicine.MedicineName, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    var mentionCount = matchingItems.Select(x => x.Source.PrescriptionId).Distinct().Count();
                    if (mentionCount == 0) mentionCount = 1;

                    var totalQty = matchingItems.Sum(x => x.Medicine.Quantity > 0 ? x.Medicine.Quantity : 1);
                    if (totalQty == 0) totalQty = mentionCount;

                    var pct = Math.Round((double)mentionCount / finalSources.Count * 100, 1);

                    return new PrescribedDrugMetricDTO
                    {
                        MedicineName = medName,
                        Category = category,
                        MentionCount = mentionCount,
                        TotalQuantity = totalQty,
                        Percentage = pct
                    };
                })
                .OrderByDescending(d => d.MentionCount)
                .ThenByDescending(d => d.TotalQuantity)
                .ToList();

            topPrescribedDrugs = groupedDrugs;

            var totalMentions = groupedDrugs.Sum(d => d.MentionCount);
            var palette = new[] { "#007671", "#0f9d76", "#2563eb", "#d97706", "#9333ea", "#e11d48", "#0891b2" };

            mostRequestedCategories = groupedDrugs
                .GroupBy(d => d.Category ?? "عام", StringComparer.OrdinalIgnoreCase)
                .Select((g, index) => new CategoryMetricDTO
                {
                    CategoryName = g.Key,
                    Count = g.Sum(d => d.MentionCount),
                    Percentage = totalMentions > 0 ? Math.Round((double)g.Sum(d => d.MentionCount) / totalMentions * 100, 1) : 0,
                    ColorHint = palette[index % palette.Length]
                })
                .OrderByDescending(c => c.Count)
                .ToList();
        }

        return new PrescriptionAnalyticsAnswerResponse
        {
            Answer = answer,
            Sources = finalSources,
            TotalPrescriptionsAnalyzed = finalSources.Count,
            TopPrescribedDrugs = topPrescribedDrugs,
            MostRequestedCategories = mostRequestedCategories
        };
    }

    private static (string Answer, string? Reasoning, List<string>? FilteredIdStrings, List<LlmMedicineCategory>? RelevantMedicines) ParseLlmResponse(string rawResponse)
    {
        if (string.IsNullOrWhiteSpace(rawResponse))
            return (rawResponse, null, null, null);

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
                return (parsed.Answer, parsed.Reasoning, parsed.RelevantPrescriptionIds, parsed.RelevantMedicines);
            }
        }
        catch
        {
            // Ignore parse errors and fallback to raw response
        }

        return (rawResponse, null, null, null);
    }

    private class PrescriptionAnalyticsLlmOutput
    {
        [JsonPropertyName("reasoning")]
        public string? Reasoning { get; set; }

        [JsonPropertyName("answer")]
        public string? Answer { get; set; }

        [JsonPropertyName("relevant_prescription_ids")]
        public List<string>? RelevantPrescriptionIds { get; set; }

        [JsonPropertyName("relevant_medicines")]
        public List<LlmMedicineCategory>? RelevantMedicines { get; set; }
    }

    private class LlmMedicineCategory
    {
        [JsonPropertyName("medicineName")]
        public string? MedicineName { get; set; }

        [JsonPropertyName("category")]
        public string? Category { get; set; }
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
