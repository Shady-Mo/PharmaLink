using Application.DTOs.AI.RAG;
using Application.Services.AI.RAG;
using Domain.Abstractions.RAG;
using Domain.Entities.RAG;
using Infrastructure.AI.Services;
using System.Diagnostics;

namespace Infrastructure.AI.RAG;

public class PrescriptionAnalyticsRagService : IPromptExecutionServiceOwner, IPrescriptionAnalyticsRagService
{
    private readonly IRagVectorStore<PrescriptionVectorIndex, PrescriptionMetadataFilter> _vectorStore;
    private readonly EmbeddingService _embeddingService;
    private readonly IPromptExecutionService _promptExecutionService;
    private readonly AppDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<PrescriptionAnalyticsRagService> _logger;

    // Internal search configuration — not exposed to API consumers
    private const int CandidateTopK = 50;
    private const double MinSimilarityThreshold = 0.0; // Raise to 0.20 after running /reindex with real embeddings

    public PrescriptionAnalyticsRagService(
        IRagVectorStore<PrescriptionVectorIndex, PrescriptionMetadataFilter> vectorStore,
        EmbeddingService embeddingService,
        IPromptExecutionService promptExecutionService,
        AppDbContext dbContext,
        IHttpContextAccessor httpContextAccessor,
        ILogger<PrescriptionAnalyticsRagService> logger)
    {
        _vectorStore = vectorStore;
        _embeddingService = embeddingService;
        _promptExecutionService = promptExecutionService;
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<PrescriptionAnalyticsRagResponseDTO> QueryAnalyticsAsync(
        PrescriptionAnalyticsRagRequestDTO request,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        if (string.IsNullOrWhiteSpace(request.Question))
            throw new ArgumentException("Question cannot be empty.", nameof(request.Question));

        // 0. Auto-index any newly added or unindexed prescriptions before executing query
        await EnsurePrescriptionsIndexedAsync(cancellationToken);

        // 1. Authorization: block competitor-pharmacy queries, resolve branchId restriction if needed
        var restrictedBranchId = await ResolveAuthorizationAsync(request, cancellationToken);

        // 2. Auto-detect intent from question text
        bool isPediatricQuery = DetectPediatricIntent(request.Question);

        // 3. Auto-detect city from question if not provided
        if (string.IsNullOrWhiteSpace(request.City))
        {
            request.City = await DetectCityFromQuestionAsync(request.Question, cancellationToken);
        }

        // 4. Prepare scope labels for response & prompt
        var regionScope = BuildRegionScope(request, restrictedBranchId);
        var timeRange = BuildTimeRange(request);

        // 5. Generate embedding for the question
        _logger.LogInformation("Generating embedding for analytics question: '{Question}'", request.Question);
        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(request.Question);

        // 6. Query vector store
        var metadataFilter = new PrescriptionMetadataFilter
        {
            RestrictedBranchId = restrictedBranchId,
            City = request.City,
            Governorate = request.Governorate,
            IsPediatric = isPediatricQuery ? true : null
        };

        var searchResults = await _vectorStore.SearchAsync(
            queryEmbedding,
            metadataFilter,
            topK: CandidateTopK,
            minSimilarity: MinSimilarityThreshold,
            cancellationToken: cancellationToken);

        _logger.LogInformation("Vector search retrieved {Count} matching prescription documents.", searchResults.Count);

        // 7. Early exit when no results — avoid wasting an LLM call
        if (searchResults.Count == 0)
        {
            stopwatch.Stop();
            return new PrescriptionAnalyticsRagResponseDTO
            {
                Answer = BuildNoDataMessage(request.Question, regionScope, timeRange),
                TotalPrescriptionsAnalyzed = 0,
                TopPrescribedDrugs = [],
                MostRequestedCategories = [],
                ShortageWarnings = [],
                DemandForecastingInsights = [],
                MatchedPrescriptions = [],
                AnalysisScope = regionScope,
                AnalysisTimeRange = timeRange,
                UsedProvider = "NoData",
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds
            };
        }

        // 8. Aggregate statistical metrics
        var topDrugs = AggregatePrescribedDrugs(searchResults);
        var categories = AggregateCategories(searchResults);
        var matchedRefs = searchResults.Select(r => new PrescriptionRefDTO
        {
            PrescriptionReviewId = r.Document.PrescriptionReviewId,
            City = r.Document.City,
            Governorate = r.Document.Governorate,
            CreatedAt = r.Document.CreatedAt,
            SimilarityScore = Math.Round(r.SimilarityScore, 3)
        }).ToList();

        // 9. Execute LLM prompt (Gemini with fallback)
        string llmAnswer;
        string usedProvider;

        try
        {
            var promptRequest = new PromptExecutionRequest
            {
                PromptName = "PrescriptionAnalyticsRAG",
                PromptVersion = "v1",
                TaskType = AITaskType.Rag,
                Variables = new Dictionary<string, object?>
                {
                    { "question", request.Question },
                    { "region_scope", regionScope },
                    { "time_range", timeRange },
                    { "prescriptions_count", searchResults.Count },
                    { "prescriptions_context", FormatPrescriptionsContext(searchResults) }
                }
            };

            var execResult = await _promptExecutionService.ExecuteAsync(promptRequest, cancellationToken);
            llmAnswer = execResult.RawResponse;
            usedProvider = $"{execResult.Provider}:{execResult.ModelId}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RAG Prompt execution failed. Providing aggregated fallback report.");
            llmAnswer = BuildFallbackReport(topDrugs);
            usedProvider = "FallbackEngine";
        }

        // 10. Shortage warnings (only meaningful when scoped to a specific branch)
        var shortageWarnings = restrictedBranchId.HasValue
            ? await EvaluateShortageWarningsAsync(topDrugs, restrictedBranchId.Value, cancellationToken)
            : [];

        // 11. Demand forecasting insights for top drugs
        var insights = topDrugs.Take(5)
            .Select(d => $"ارتفاع الطلب على {d.MedicineName}: ذُكر في {d.MentionCount} روشتة ({d.Percentage}% من الإجمالي)، بإجمالي كمية {d.TotalQuantity} عبوة.")
            .ToList();

        stopwatch.Stop();

        return new PrescriptionAnalyticsRagResponseDTO
        {
            Answer = llmAnswer,
            TotalPrescriptionsAnalyzed = searchResults.Count,
            TopPrescribedDrugs = topDrugs,
            MostRequestedCategories = categories,
            ShortageWarnings = shortageWarnings,
            DemandForecastingInsights = insights,
            MatchedPrescriptions = matchedRefs,
            AnalysisScope = regionScope,
            AnalysisTimeRange = timeRange,
            UsedProvider = usedProvider,
            ExecutionTimeMs = stopwatch.ElapsedMilliseconds
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Re-indexing
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<int> ReindexPrescriptionsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting bulk re-indexing of prescription reviews...");

        var reviews = await _dbContext.PrescriptionReviews
            .Include(r => r.Medicines)
            .Include(r => r.Patient)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        int indexedCount = 0;
        foreach (var review in reviews)
        {
            await IndexPrescriptionInternalAsync(review, cancellationToken);
            indexedCount++;
        }

        _logger.LogInformation("Bulk re-indexing completed. Total indexed: {Count}", indexedCount);
        return indexedCount;
    }

    public async Task IndexSinglePrescriptionAsync(Guid prescriptionReviewId, CancellationToken cancellationToken = default)
    {
        var review = await _dbContext.PrescriptionReviews
            .Include(r => r.Medicines)
            .Include(r => r.Patient)
            .FirstOrDefaultAsync(r => r.PrescriptionReviewId == prescriptionReviewId, cancellationToken);

        if (review != null)
            await IndexPrescriptionInternalAsync(review, cancellationToken);
    }

    public async Task<int> EnsurePrescriptionsIndexedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Checking for unindexed prescription reviews in vector store...");

        var candidateReviewIds = await _dbContext.PrescriptionReviews
            .AsNoTracking()
            .Where(r => r.Medicines.Any() || !string.IsNullOrWhiteSpace(r.ExtractedText))
            .Select(r => r.PrescriptionReviewId)
            .ToListAsync(cancellationToken);

        if (candidateReviewIds.Count == 0)
        {
            return 0;
        }

        var indexedReviewIds = await _dbContext.PrescriptionVectorIndices
            .AsNoTracking()
            .Select(v => v.PrescriptionReviewId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var unindexedIds = candidateReviewIds.Except(indexedReviewIds).ToList();

        if (unindexedIds.Count == 0)
        {
            _logger.LogInformation("All prescription reviews are already indexed in vector store.");
            return 0;
        }

        _logger.LogInformation("Found {Count} unindexed prescription review(s). Generating embeddings...", unindexedIds.Count);

        int newlyIndexed = 0;
        foreach (var reviewId in unindexedIds)
        {
            var review = await _dbContext.PrescriptionReviews
                .Include(r => r.Medicines)
                .Include(r => r.Patient)
                .FirstOrDefaultAsync(r => r.PrescriptionReviewId == reviewId, cancellationToken);

            if (review != null)
            {
                await IndexPrescriptionInternalAsync(review, cancellationToken);
                newlyIndexed++;
            }
        }

        _logger.LogInformation("Auto-indexing complete. Successfully indexed {Count} new prescription review(s).", newlyIndexed);
        return newlyIndexed;
    }

    private async Task IndexPrescriptionInternalAsync(PrescriptionReview review, CancellationToken cancellationToken)
    {
        if (review.Medicines.Count == 0 && string.IsNullOrWhiteSpace(review.ExtractedText))
            return;

        var medicinesList = review.Medicines.Select(m => new
        {
            m.MedicineName,
            m.GenericName,
            m.DosageForm,
            m.Strength,
            m.Quantity,
            IsPediatric = IsPediatricMedicine(m.MedicineName, m.DosageForm)
        }).ToList();

        bool isPediatric = medicinesList.Any(m => m.IsPediatric);

        string textSummary = $"روشتة بتاريخ {review.CreatedAt:yyyy-MM-dd}. الأدوية: " +
            string.Join(", ", medicinesList.Select(m => $"{m.MedicineName} ({m.GenericName ?? "عام"}) {m.Strength} {m.DosageForm}"));

        var embedding = await _embeddingService.GenerateEmbeddingAsync(textSummary);

        var patientAddress = await _dbContext.Addresses.AsNoTracking()
            .Where(a => a.UserId == review.PatientUserId)
            .OrderByDescending(a => a.IsDefault)
            .FirstOrDefaultAsync(cancellationToken);

        string city = !string.IsNullOrWhiteSpace(patientAddress?.City) ? patientAddress.City : "القاهرة";
        string governorate = !string.IsNullOrWhiteSpace(patientAddress?.Governorate) ? patientAddress.Governorate : "القاهرة";

        Guid? branchId = null;
        if (review.PharmacistUserId.HasValue)
        {
            var assignment = await _dbContext.PharmacistAssignments.AsNoTracking()
                .Where(pa => pa.PharmacistId == review.PharmacistUserId.Value && pa.IsActive)
                .FirstOrDefaultAsync(cancellationToken);
            branchId = assignment?.BranchId;
        }

        var indexRecord = new PrescriptionVectorIndex
        {
            PrescriptionVectorIndexId = Guid.NewGuid(),
            PrescriptionReviewId = review.PrescriptionReviewId,
            BranchId = branchId,
            City = city,
            Governorate = governorate,
            IndexedText = textSummary,
            EmbeddingJson = JsonSerializer.Serialize(embedding.ToArray()),
            MedicinesJson = JsonSerializer.Serialize(medicinesList),
            IsPediatric = isPediatric,
            CreatedAt = review.CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };

        await _vectorStore.IndexAsync(indexRecord, cancellationToken);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Authorization
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a restrictedBranchId (non-null) only when the user explicitly requests
    /// a specific branch they own/work at. Returns null for all market-wide queries.
    /// Throws UnauthorizedAccessException if the user tries to access a competitor's branch.
    /// </summary>
    private async Task<Guid?> ResolveAuthorizationAsync(
        PrescriptionAnalyticsRagRequestDTO request,
        CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User == null) return null;

        var userIdStr = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userIdStr, out var userId)) return null;

        // SystemAdmin: full unrestricted access
        if (httpContext.User.IsInRole(AppRoles.Admin))
        {
            return (request.BranchId.HasValue && request.BranchId.Value != Guid.Empty)
                ? request.BranchId.Value
                : null;
        }

        var isPharmacist = httpContext.User.IsInRole(AppRoles.Pharmacist);
        var isPharmacyAdmin = httpContext.User.IsInRole(AppRoles.PharmacyAdmin);

        if (!isPharmacist && !isPharmacyAdmin) return null;

        // Detect branchId from question text if user didn't provide one in DTO
        if (!request.BranchId.HasValue || request.BranchId.Value == Guid.Empty)
        {
            var mentionedBranch = await _dbContext.PharmacyBranches.AsNoTracking()
                .FirstOrDefaultAsync(b => request.Question.Contains(b.BranchName), cancellationToken);

            if (mentionedBranch != null)
                request.BranchId = mentionedBranch.BranchId;
        }

        // No branchId → general market query → no restriction
        if (!request.BranchId.HasValue || request.BranchId.Value == Guid.Empty)
            return null;

        // BranchId provided → verify ownership/assignment
        var requestedBranchId = request.BranchId.Value;

        if (isPharmacist)
        {
            bool hasAccess = await _dbContext.PharmacistAssignments.AsNoTracking()
                .AnyAsync(pa => pa.PharmacistId == userId && pa.BranchId == requestedBranchId && pa.IsActive, cancellationToken);

            if (!hasAccess)
                throw new UnauthorizedAccessException(
                    "غير مسموح لك بالوصول لاستعلامات أو تحليلات فرع صيدلية غير مخصص لك.");

            return requestedBranchId;
        }

        if (isPharmacyAdmin)
        {
            bool hasAccess = await _dbContext.PharmacyBranches.AsNoTracking()
                .AnyAsync(b => b.BranchId == requestedBranchId && b.Pharmacy.Admins.Any(a => a.Id == userId), cancellationToken);

            if (!hasAccess)
                throw new UnauthorizedAccessException(
                    "غير مسموح لك بالوصول لبيانات أو تحليلات فروع صيدليات لا تملكها.");

            return requestedBranchId;
        }

        return null;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Aggregation helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static List<PrescribedDrugMetricDTO> AggregatePrescribedDrugs(
        IReadOnlyList<VectorSearchResult<PrescriptionVectorIndex>> searchResults)
    {
        var dict = new Dictionary<string, PrescribedDrugMetricDTO>(StringComparer.OrdinalIgnoreCase);
        int total = searchResults.Count;

        foreach (var res in searchResults)
        {
            try
            {
                using var doc = JsonDocument.Parse(res.Document.MedicinesJson);
                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    var name = element.GetProperty("MedicineName").GetString() ?? "دواء غير معروف";
                    var generic = element.TryGetProperty("GenericName", out var g) ? g.GetString() : null;
                    int qty = element.TryGetProperty("Quantity", out var q) ? q.GetInt32() : 1;
                    bool pediatric = element.TryGetProperty("IsPediatric", out var p) && p.GetBoolean();

                    if (!dict.TryGetValue(name, out var metric))
                    {
                        metric = new PrescribedDrugMetricDTO
                        {
                            MedicineName = name,
                            GenericName = generic,
                            MentionCount = 0,
                            TotalQuantity = 0,
                            IsPediatric = pediatric,
                            Trend = "stable"
                        };
                        dict[name] = metric;
                    }

                    metric.MentionCount++;
                    metric.TotalQuantity += qty;
                }
            }
            catch { /* Soft handle malformed JSON */ }
        }

        var list = dict.Values
            .OrderByDescending(x => x.MentionCount)
            .ThenByDescending(x => x.TotalQuantity)
            .ToList();

        foreach (var item in list)
        {
            item.Percentage = total > 0
                ? Math.Round((item.MentionCount / (double)total) * 100, 1)
                : 0.0;
        }

        return list;
    }

    private static List<CategoryMetricDTO> AggregateCategories(
        IReadOnlyList<VectorSearchResult<PrescriptionVectorIndex>> searchResults)
    {
        int total = searchResults.Count;
        int pediatricCount = searchResults.Count(r => r.Document.IsPediatric);
        int adultCount = total - pediatricCount;
        int antibioticEstimate = (int)(adultCount * 0.35);
        int chronicEstimate = adultCount - antibioticEstimate;

        return
        [
            new CategoryMetricDTO
            {
                CategoryName = "أدوية الأطفال",
                Count = pediatricCount,
                Percentage = total > 0 ? Math.Round((pediatricCount / (double)total) * 100, 1) : 0.0,
                ColorHint = "#4CAF50"
            },
            new CategoryMetricDTO
            {
                CategoryName = "مضادات حيوية ومسكنات",
                Count = antibioticEstimate,
                Percentage = total > 0 ? Math.Round((antibioticEstimate / (double)total) * 100, 1) : 0.0,
                ColorHint = "#FF9800"
            },
            new CategoryMetricDTO
            {
                CategoryName = "أمراض مزمنة (سكر وضغط)",
                Count = chronicEstimate,
                Percentage = total > 0 ? Math.Round((chronicEstimate / (double)total) * 100, 1) : 0.0,
                ColorHint = "#2196F3"
            }
        ];
    }

    private async Task<List<ShortageWarningDTO>> EvaluateShortageWarningsAsync(
        List<PrescribedDrugMetricDTO> topDrugs,
        Guid branchId,
        CancellationToken cancellationToken)
    {
        var warnings = new List<ShortageWarningDTO>();

        foreach (var drug in topDrugs.Take(5))
        {
            var inventoryItem = await _dbContext.PharmacyInventories.AsNoTracking()
                .Include(i => i.Drug)
                .FirstOrDefaultAsync(i =>
                    i.BranchId == branchId &&
                    (i.Drug.BrandName.Contains(drug.MedicineName) || i.Drug.ArabicName.Contains(drug.MedicineName)),
                    cancellationToken);

            int currentStock = inventoryItem?.StockQuantity ?? 0;

            if (currentStock < drug.TotalQuantity * 2)
            {
                string urgency = currentStock == 0 ? "Critical" : (currentStock < drug.TotalQuantity ? "High" : "Medium");

                warnings.Add(new ShortageWarningDTO
                {
                    DrugName = drug.MedicineName,
                    HighDemandReason = $"مطلوب في {drug.MentionCount} روشتة بإجمالي {drug.TotalQuantity} عبوة ({drug.Percentage}% من الروشتات).",
                    AvailableStock = currentStock,
                    Recommendation = $"يُنصح بطلب ما لا يقل عن {drug.TotalQuantity * 3} عبوة للفرع.",
                    UrgencyLevel = urgency
                });
            }
        }

        return warnings;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Text / Formatting helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static bool DetectPediatricIntent(string question) =>
        question.Contains("أطفال") || question.Contains("طفل") ||
        question.Contains("رضيع") || question.Contains("شراب") ||
        question.Contains("نقط") || question.Contains("pediatric") ||
        question.Contains("child");

    private async Task<string?> DetectCityFromQuestionAsync(string question, CancellationToken cancellationToken)
    {
        var cities = await _dbContext.PrescriptionVectorIndices.AsNoTracking()
            .Select(x => x.City)
            .Distinct()
            .ToListAsync(cancellationToken);

        return cities.FirstOrDefault(c =>
            !string.IsNullOrWhiteSpace(c) &&
            question.Contains(c, StringComparison.OrdinalIgnoreCase));
    }

    private static string BuildRegionScope(PrescriptionAnalyticsRagRequestDTO request, Guid? restrictedBranchId)
    {
        if (restrictedBranchId.HasValue)
            return $"فرع محدد (BranchId: {restrictedBranchId.Value})";

        if (!string.IsNullOrWhiteSpace(request.City) && !string.IsNullOrWhiteSpace(request.Governorate))
            return $"مدينة {request.City} - محافظة {request.Governorate}";

        if (!string.IsNullOrWhiteSpace(request.City))
            return $"مدينة {request.City}";

        if (!string.IsNullOrWhiteSpace(request.Governorate))
            return $"محافظة {request.Governorate}";

        return "جميع المدن والمناطق (بحث سوق عام)";
    }

    private static string BuildTimeRange(PrescriptionAnalyticsRagRequestDTO request)
    {
        if (request.StartDate.HasValue && request.EndDate.HasValue)
            return $"من {request.StartDate.Value:yyyy-MM-dd} إلى {request.EndDate.Value:yyyy-MM-dd}";

        if (request.StartDate.HasValue)
            return $"من {request.StartDate.Value:yyyy-MM-dd} حتى الآن";

        return "جميع الفترات المتاحة";
    }

    private static string FormatPrescriptionsContext(IReadOnlyList<VectorSearchResult<PrescriptionVectorIndex>> results)
    {
        if (results.Count == 0) return "لا توجد روشتات مطابقة.";

        var sb = new System.Text.StringBuilder();
        int i = 1;
        foreach (var res in results)
        {
            sb.AppendLine($"[روشتة #{i}] (تشابه: {res.SimilarityScore:F2} | مدينة: {res.Document.City} | تاريخ: {res.Document.CreatedAt:yyyy-MM-dd})");
            sb.AppendLine($"  {res.Document.IndexedText}");
            sb.AppendLine();
            i++;
        }
        return sb.ToString();
    }

    private static string BuildNoDataMessage(string question, string regionScope, string timeRange) =>
        $"لا توجد روشتات مسجلة في النظام تطابق معايير بحثك.\n\n" +
        $"**السؤال:** {question}\n" +
        $"**النطاق:** {regionScope}\n" +
        $"**الفترة:** {timeRange}\n\n" +
        "**اقتراح:** جرب توسيع الفترة الزمنية، أو تغيير المدينة/المنطقة، أو التأكد من تشغيل عملية الـ Reindex.";

    private static string BuildFallbackReport(List<PrescribedDrugMetricDTO> topDrugs)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"تم تحليل الروشتات المتاحة. إليك أبرز النتائج:");
        sb.AppendLine();

        if (topDrugs.Count > 0)
        {
            sb.AppendLine("💊 **الأدوية الأكثر طلباً:**");
            foreach (var drug in topDrugs.Take(5))
                sb.AppendLine($"- **{drug.MedicineName}** ({drug.GenericName ?? "عام"}): {drug.MentionCount} روشتة | {drug.TotalQuantity} عبوة | {drug.Percentage}%");
        }

        return sb.ToString();
    }

    private static bool IsPediatricMedicine(string medicineName, string? dosageForm)
    {
        var lower = medicineName.ToLowerInvariant();
        var formLower = (dosageForm ?? "").ToLowerInvariant();

        return lower.Contains("pediatric") || lower.Contains("drops") ||
               lower.Contains("syrup") || lower.Contains("suspension") ||
               formLower.Contains("syrup") || formLower.Contains("drops") ||
               formLower.Contains("شراب") || formLower.Contains("نقط") ||
               formLower.Contains("أطفال");
    }
}

public interface IPromptExecutionServiceOwner { }
