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

        // 2. Auto-detect time range from question if not provided in DTO
        if (!request.StartDate.HasValue && !request.EndDate.HasValue)
        {
            var (detectedStart, detectedEnd) = DetectTimeRangeFromQuestion(request.Question);
            if (detectedStart.HasValue)
            {
                request.StartDate = detectedStart;
                request.EndDate = detectedEnd;
            }
        }

        // 3. Auto-detect category intent & pediatric flag from question text
        var categoryIntent = DetectCategoryIntent(request.Question);
        bool isPediatricQuery = categoryIntent == CategoryIntent.Pediatric || DetectPediatricIntent(request.Question);

        // 4. Auto-detect city & governorate from question if not provided
        if (string.IsNullOrWhiteSpace(request.City))
        {
            request.City = await DetectCityFromQuestionAsync(request.Question, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(request.Governorate))
        {
            request.Governorate = await DetectGovernorateFromQuestionAsync(request.Question, cancellationToken);
        }

        // 5. Prepare scope labels for response & prompt
        var regionScope = BuildRegionScope(request, restrictedBranchId);
        var timeRange = BuildTimeRange(request);

        // 6. Generate embedding for the question
        _logger.LogInformation("Generating embedding for analytics question: '{Question}'", request.Question);
        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(request.Question);

        // 7. Query vector store
        var metadataFilter = new PrescriptionMetadataFilter
        {
            RestrictedBranchId = restrictedBranchId,
            City = request.City,
            Governorate = request.Governorate,
            IsPediatric = isPediatricQuery ? true : null,
            StartDate = request.StartDate,
            EndDate = request.EndDate
        };

        var searchResults = await _vectorStore.SearchAsync(
            queryEmbedding,
            metadataFilter,
            topK: CandidateTopK,
            minSimilarity: MinSimilarityThreshold,
            cancellationToken: cancellationToken);

        _logger.LogInformation("Vector search retrieved {Count} matching prescription documents.", searchResults.Count);

        // 8. Early exit when no results — avoid wasting an LLM call
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

        // 9. Aggregate statistical metrics
        var topDrugs = AggregatePrescribedDrugs(searchResults, categoryIntent);
        var categories = AggregateCategories(searchResults, categoryIntent, topDrugs);
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
        IReadOnlyList<VectorSearchResult<PrescriptionVectorIndex>> searchResults,
        CategoryIntent categoryIntent = CategoryIntent.None)
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
                    var dosageForm = element.TryGetProperty("DosageForm", out var df) ? df.GetString() : null;
                    int qty = element.TryGetProperty("Quantity", out var q) ? q.GetInt32() : 1;
                    bool pediatric = (element.TryGetProperty("IsPediatric", out var p) && p.GetBoolean())
                                     || IsPediatricMedicine(name, dosageForm);

                    if (categoryIntent != CategoryIntent.None && !MatchesCategoryIntent(name, generic, dosageForm, categoryIntent))
                    {
                        continue;
                    }

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
        IReadOnlyList<VectorSearchResult<PrescriptionVectorIndex>> searchResults,
        CategoryIntent categoryIntent,
        List<PrescribedDrugMetricDTO> topDrugs)
    {
        if (topDrugs.Count == 0) return [];

        int totalMedicinesCount = topDrugs.Sum(d => d.MentionCount);
        if (totalMedicinesCount == 0) totalMedicinesCount = 1;

        if (categoryIntent == CategoryIntent.Diabetes)
        {
            int oralCount = topDrugs.Where(d => IsOralDiabetesMedicine(d.MedicineName, d.GenericName)).Sum(d => d.MentionCount);
            int insulinCount = topDrugs.Where(d => IsInsulinOrSyringe(d.MedicineName, d.GenericName)).Sum(d => d.MentionCount);
            int otherCount = totalMedicinesCount - (oralCount + insulinCount);
            if (otherCount < 0) otherCount = 0;

            var list = new List<CategoryMetricDTO>();
            if (oralCount > 0)
                list.Add(new CategoryMetricDTO { CategoryName = "أدوية السكر الفموية", Count = oralCount, Percentage = Math.Round((oralCount / (double)totalMedicinesCount) * 100, 1), ColorHint = "#2196F3" });
            if (insulinCount > 0)
                list.Add(new CategoryMetricDTO { CategoryName = "مستلزمات وحقن الإنسولين", Count = insulinCount, Percentage = Math.Round((insulinCount / (double)totalMedicinesCount) * 100, 1), ColorHint = "#00BCD4" });
            if (otherCount > 0)
                list.Add(new CategoryMetricDTO { CategoryName = "مستحضرات سكر أخرى", Count = otherCount, Percentage = Math.Round((otherCount / (double)totalMedicinesCount) * 100, 1), ColorHint = "#9C27B0" });

            return list;
        }

        if (categoryIntent == CategoryIntent.Hypertension)
        {
            int bpCount = topDrugs.Where(d => IsHypertensionMedicine(d.MedicineName, d.GenericName)).Sum(d => d.MentionCount);
            int cardiacCount = totalMedicinesCount - bpCount;
            if (cardiacCount < 0) cardiacCount = 0;

            var list = new List<CategoryMetricDTO>();
            if (bpCount > 0)
                list.Add(new CategoryMetricDTO { CategoryName = "أدوية خفض ضغط الدم", Count = bpCount, Percentage = Math.Round((bpCount / (double)totalMedicinesCount) * 100, 1), ColorHint = "#2196F3" });
            if (cardiacCount > 0)
                list.Add(new CategoryMetricDTO { CategoryName = "أدوية القلب والأوعية", Count = cardiacCount, Percentage = Math.Round((cardiacCount / (double)totalMedicinesCount) * 100, 1), ColorHint = "#E91E63" });

            return list;
        }

        if (categoryIntent == CategoryIntent.Pediatric)
        {
            int suppCount = topDrugs.Where(d => (d.MedicineName ?? "").ToLower().Contains("supp") || (d.GenericName ?? "").ToLower().Contains("supp") || d.MedicineName.Contains("لبوس")).Sum(d => d.MentionCount);
            int syrupCount = totalMedicinesCount - suppCount;
            if (syrupCount < 0) syrupCount = 0;

            var list = new List<CategoryMetricDTO>();
            if (syrupCount > 0)
                list.Add(new CategoryMetricDTO { CategoryName = "أشربة وقطرات أطفال", Count = syrupCount, Percentage = Math.Round((syrupCount / (double)totalMedicinesCount) * 100, 1), ColorHint = "#4CAF50" });
            if (suppCount > 0)
                list.Add(new CategoryMetricDTO { CategoryName = "تحاميل ولبوس أطفال", Count = suppCount, Percentage = Math.Round((suppCount / (double)totalMedicinesCount) * 100, 1), ColorHint = "#FF9800" });

            return list;
        }

        if (categoryIntent == CategoryIntent.Cosmetics)
        {
            int hairCount = topDrugs.Where(d => (d.MedicineName ?? "").ToLower().Contains("hair") || d.MedicineName.Contains("شعر") || (d.MedicineName ?? "").ToLower().Contains("vatika")).Sum(d => d.MentionCount);
            int skinCount = totalMedicinesCount - hairCount;
            if (skinCount < 0) skinCount = 0;

            var list = new List<CategoryMetricDTO>();
            if (hairCount > 0)
                list.Add(new CategoryMetricDTO { CategoryName = "مستحضرات العناية بالشعر", Count = hairCount, Percentage = Math.Round((hairCount / (double)totalMedicinesCount) * 100, 1), ColorHint = "#E91E63" });
            if (skinCount > 0)
                list.Add(new CategoryMetricDTO { CategoryName = "مستحضرات العناية بالبشرة", Count = skinCount, Percentage = Math.Round((skinCount / (double)totalMedicinesCount) * 100, 1), ColorHint = "#9C27B0" });

            return list;
        }

        if (categoryIntent == CategoryIntent.Supplies)
        {
            int antisepticsCount = topDrugs.Where(d => (d.MedicineName ?? "").ToLower().Contains("peroxide") || d.MedicineName.Contains("مطهر")).Sum(d => d.MentionCount);
            int cottonGauzeCount = totalMedicinesCount - antisepticsCount;
            if (cottonGauzeCount < 0) cottonGauzeCount = 0;

            var list = new List<CategoryMetricDTO>();
            if (cottonGauzeCount > 0)
                list.Add(new CategoryMetricDTO { CategoryName = "ضمادات وقطن طبي", Count = cottonGauzeCount, Percentage = Math.Round((cottonGauzeCount / (double)totalMedicinesCount) * 100, 1), ColorHint = "#00BCD4" });
            if (antisepticsCount > 0)
                list.Add(new CategoryMetricDTO { CategoryName = "مطهرات وسوائل طبية", Count = antisepticsCount, Percentage = Math.Round((antisepticsCount / (double)totalMedicinesCount) * 100, 1), ColorHint = "#4CAF50" });

            return list;
        }

        // For CategoryIntent.None (General market queries)
        int pediatric = topDrugs.Where(d => IsPediatricMedicine(d.MedicineName, null)).Sum(d => d.MentionCount);
        int cosmetics = topDrugs.Where(d => IsPersonalCareOrCosmetics(d.MedicineName, d.GenericName, null)).Sum(d => d.MentionCount);
        int supplies = topDrugs.Where(d => IsMedicalSupplies(d.MedicineName, d.GenericName, null)).Sum(d => d.MentionCount);
        int antibiotic = topDrugs.Where(d => IsAntibioticOrPainkiller(d.MedicineName, d.GenericName)).Sum(d => d.MentionCount);
        int chronic = topDrugs.Where(d => IsChronicMedicine(d.MedicineName, d.GenericName)).Sum(d => d.MentionCount);
        int others = totalMedicinesCount - (pediatric + cosmetics + supplies + antibiotic + chronic);
        if (others < 0) others = 0;

        var generalList = new List<CategoryMetricDTO>();
        if (pediatric > 0) generalList.Add(new CategoryMetricDTO { CategoryName = "أدوية الأطفال والرضع", Count = pediatric, Percentage = Math.Round((pediatric / (double)totalMedicinesCount) * 100, 1), ColorHint = "#4CAF50" });
        if (cosmetics > 0) generalList.Add(new CategoryMetricDTO { CategoryName = "مستحضرات تجميل وعناية شخصية", Count = cosmetics, Percentage = Math.Round((cosmetics / (double)totalMedicinesCount) * 100, 1), ColorHint = "#E91E63" });
        if (supplies > 0) generalList.Add(new CategoryMetricDTO { CategoryName = "مستلزمات ومطهرات طبية", Count = supplies, Percentage = Math.Round((supplies / (double)totalMedicinesCount) * 100, 1), ColorHint = "#00BCD4" });
        if (antibiotic > 0) generalList.Add(new CategoryMetricDTO { CategoryName = "مضادات حيوية ومسكنات", Count = antibiotic, Percentage = Math.Round((antibiotic / (double)totalMedicinesCount) * 100, 1), ColorHint = "#FF9800" });
        if (chronic > 0) generalList.Add(new CategoryMetricDTO { CategoryName = "أمراض مزمنة (سكر وضغط وقلب)", Count = chronic, Percentage = Math.Round((chronic / (double)totalMedicinesCount) * 100, 1), ColorHint = "#2196F3" });
        if (others > 0) generalList.Add(new CategoryMetricDTO { CategoryName = "أدوية وفيتامينات أخرى", Count = others, Percentage = Math.Round((others / (double)totalMedicinesCount) * 100, 1), ColorHint = "#9C27B0" });

        return generalList;
    }

    private static bool IsPersonalCareOrCosmetics(string name, string? generic, string? dosageForm)
    {
        var text = (name + " " + (generic ?? "") + " " + (dosageForm ?? "")).ToLowerInvariant();

        return text.Contains("cream") || text.Contains("lotion") || text.Contains("shampoo") ||
               text.Contains("hair") || text.Contains("vatika") || text.Contains("bless") ||
               text.Contains("skin") || text.Contains("serum") || text.Contains("soap") ||
               text.Contains("كريم") || text.Contains("شامبو") || text.Contains("شعر") ||
               text.Contains("بشرة") || text.Contains("عناية") || text.Contains("تجميل");
    }

    private static bool IsMedicalSupplies(string name, string? generic, string? dosageForm)
    {
        var text = (name + " " + (generic ?? "") + " " + (dosageForm ?? "")).ToLowerInvariant();

        return text.Contains("cotton") || text.Contains("tape") || text.Contains("pad") ||
               text.Contains("bandage") || text.Contains("peroxide") || text.Contains("syringe") ||
               text.Contains("needle") || text.Contains("gauze") || text.Contains("surgipad") ||
               text.Contains("silkplast") || text.Contains("super lord") || text.Contains("cmi") ||
               text.Contains("قطن") || text.Contains("بلاستر") || text.Contains("ضمادة") ||
               text.Contains("شاش") || text.Contains("سرنجة") || text.Contains("مطهر");
    }

    private static bool IsAntibioticOrPainkiller(string name, string? generic)
    {
        var text = (name + " " + (generic ?? "")).ToLowerInvariant();

        return text.Contains("amox") || text.Contains("amoxil") || text.Contains("flucamox") ||
               text.Contains("cef") || text.Contains("curisafe") || text.Contains("cifin") ||
               text.Contains("cipro") || text.Contains("azithro") || text.Contains("augmentin") ||
               text.Contains("gentamicin") || text.Contains("epigent") || text.Contains("antibiotic") ||
               text.Contains("diclofenac") || text.Contains("declophen") || text.Contains("epifenac") ||
               text.Contains("brufen") || text.Contains("ibuprofen") || text.Contains("paracetamol") ||
               text.Contains("ketofan") || text.Contains("ketoprek") || text.Contains("actifast") ||
               text.Contains("rheumarene") || text.Contains("adwiflam") || text.Contains("analgesic") ||
               text.Contains("مضاد حيوي") || text.Contains("مسكن") || text.Contains("مضاد للألم");
    }

    private static bool IsChronicMedicine(string name, string? generic)
    {
        var text = (name + " " + (generic ?? "")).ToLowerInvariant();

        return text.Contains("metformin") || text.Contains("cidophage") || text.Contains("amophage") ||
               text.Contains("diabenor") || text.Contains("daonil") || text.Contains("insulin") ||
               text.Contains("bisoprolol") || text.Contains("bisolock") || text.Contains("sinopril") ||
               text.Contains("enalapril") || text.Contains("atenolol") || text.Contains("blokium") ||
               text.Contains("aldactone") || text.Contains("diltiazem") || text.Contains("delay tiazem") ||
               text.Contains("rampecardin") || text.Contains("olmeborg") || text.Contains("hypertension") ||
               text.Contains("diabetes") || text.Contains("سكر") || text.Contains("ضغط");
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
        var normQuestion = NormalizeArabicText(question);

        // 1. Known Egyptian city aliases dictionary
        var knownCityAliases = new (string Key, string Value)[]
        {
            ("شبرا الخيمة", "شبر الخيمة"),
            ("شبر الخيمة", "شبر الخيمة"),
            ("شبرا", "شبرا"),
            ("طنطا", "طنطا"),
            ("اسكندرية", "الإسكندرية"),
            ("الاسكندرية", "الإسكندرية"),
            ("إسكندرية", "الإسكندرية"),
            ("الإسكندرية", "الإسكندرية"),
            ("المنصورة", "المنصورة"),
            ("المحلة الكبرى", "المحلة الكبرى"),
            ("المحلة", "المحلة الكبرى"),
            ("الزقازيق", "الزقازيق"),
            ("شبين الكوم", "شبين الكوم"),
            ("شبين", "شبين الكوم"),
            ("دمنهور", "دمنهور"),
            ("بنها", "بنها"),
            ("بورسعيد", "بورسعيد"),
            ("السويس", "السويس"),
            ("الاسماعيلية", "الإسماعيلية"),
            ("الإسماعيلية", "الإسماعيلية"),
            ("القاهرة", "القاهرة"),
            ("الجيزة", "الجيزة")
        };

        foreach (var (key, value) in knownCityAliases)
        {
            if (normQuestion.Contains(NormalizeArabicText(key)))
                return value;
        }

        // 2. Dynamic check against database cities
        var dbCities = await _dbContext.PrescriptionVectorIndices.AsNoTracking()
            .Select(x => x.City)
            .Union(_dbContext.Addresses.AsNoTracking().Select(a => a.City))
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var dbCity in dbCities)
        {
            var normDbCity = NormalizeArabicText(dbCity);
            if (!string.IsNullOrWhiteSpace(normDbCity) && (normQuestion.Contains(normDbCity) || normDbCity.Contains(normQuestion)))
                return dbCity;
        }

        return null;
    }

    private async Task<string?> DetectGovernorateFromQuestionAsync(string question, CancellationToken cancellationToken)
    {
        var normQuestion = NormalizeArabicText(question);

        var knownGovs = new (string Key, string Value)[]
        {
            ("الغربية", "الغربية"),
            ("الإسكندرية", "الإسكندرية"),
            ("الاسكندرية", "الإسكندرية"),
            ("اسكندرية", "الإسكندرية"),
            ("القاهرة", "القاهرة"),
            ("الجيزة", "الجيزة"),
            ("القليوبية", "القليوبية"),
            ("الدقهلية", "الدقهلية"),
            ("الشرقية", "الشرقية"),
            ("المنوفية", "المنوفية"),
            ("البحيرة", "البحيرة"),
            ("كفر الشيخ", "كفر الشيخ"),
            ("دمياط", "دمياط"),
            ("بني سويف", "بني سويف"),
            ("الفيوم", "الفيوم"),
            ("المنيا", "المنيا"),
            ("أسيوط", "أسيوط"),
            ("سوهاج", "سوهاج"),
            ("قنا", "قنا"),
            ("الأقصر", "الأقصر"),
            ("أسوان", "أسوان"),
            ("السويس", "السويس"),
            ("بورسعيد", "بورسعيد"),
            ("الإسماعيلية", "الإسماعيلية")
        };

        foreach (var (key, value) in knownGovs)
        {
            if (normQuestion.Contains(NormalizeArabicText(key)))
                return value;
        }

        var dbGovs = await _dbContext.PrescriptionVectorIndices.AsNoTracking()
            .Select(x => x.Governorate)
            .Union(_dbContext.Addresses.AsNoTracking().Select(a => a.Governorate))
            .Where(g => !string.IsNullOrWhiteSpace(g))
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var dbGov in dbGovs)
        {
            var normDbGov = NormalizeArabicText(dbGov);
            if (!string.IsNullOrWhiteSpace(normDbGov) && (normQuestion.Contains(normDbGov) || normDbGov.Contains(normQuestion)))
                return dbGov;
        }

        return null;
    }

    private static string NormalizeArabicText(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        return input.Trim()
            .Replace('أ', 'ا')
            .Replace('إ', 'ا')
            .Replace('آ', 'ا')
            .Replace('ة', 'ه')
            .Replace('ى', 'ي')
            .ToLowerInvariant();
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

        if (lower.Contains("pediatric") || lower.Contains("drops") ||
            lower.Contains("syrup") || lower.Contains("suspension") ||
            lower.Contains("infant") || lower.Contains("baby") ||
            lower.Contains("child") || lower.Contains("kid") ||
            lower.Contains("أطفال") || lower.Contains("اطفال") ||
            lower.Contains("رضع") || lower.Contains("بيبي") ||
            formLower.Contains("syrup") || formLower.Contains("drops") ||
            formLower.Contains("suspension") || formLower.Contains("شراب") ||
            formLower.Contains("نقط") || formLower.Contains("معلق") ||
            formLower.Contains("أطفال") || formLower.Contains("اطفال"))
        {
            return true;
        }

        bool isSuppository = lower.Contains("supp") || lower.Contains("suppositories") ||
                             formLower.Contains("supp") || formLower.Contains("لبوس") ||
                             formLower.Contains("تحاميل") || formLower.Contains("اقماع");

        if (isSuppository)
        {
            if (lower.Contains("12.5") || lower.Contains("25mg") || lower.Contains("25 mg") ||
                lower.Contains("0.7") || lower.Contains("infant") || lower.Contains("pediatric") ||
                lower.Contains("أطفال") || lower.Contains("اطفال"))
            {
                return true;
            }
        }

        return false;
    }

    private static CategoryIntent DetectCategoryIntent(string question)
    {
        if (string.IsNullOrWhiteSpace(question)) return CategoryIntent.None;
        var q = question.ToLowerInvariant();

        if (q.Contains("سكر") || q.Contains("سكري") || q.Contains("انسولين") || q.Contains("إنسولين") || q.Contains("diabetes") || q.Contains("diabetic"))
            return CategoryIntent.Diabetes;

        if (q.Contains("ضغط") || q.Contains("قلب") || q.Contains("hypertension") || q.Contains("cardiac"))
            return CategoryIntent.Hypertension;

        if (q.Contains("أطفال") || q.Contains("اطفال") || q.Contains("رضع") || q.Contains("رضيع") || q.Contains("طفل") || q.Contains("بيبي") || q.Contains("pediatric"))
            return CategoryIntent.Pediatric;

        if (q.Contains("مضاد") || q.Contains("مضادات") || q.Contains("antibiotic"))
            return CategoryIntent.Antibiotic;

        if (q.Contains("مسكن") || q.Contains("مسكنات") || q.Contains("عظام") || q.Contains("روماتيزم") || q.Contains("صداع") || q.Contains("painkiller") || q.Contains("analgesic"))
            return CategoryIntent.Painkiller;

        if (q.Contains("شعر") || q.Contains("بشرة") || q.Contains("تجميل") || q.Contains("عناية") || q.Contains("كريمات") || q.Contains("شامبو") || q.Contains("cosmetics") || q.Contains("hair"))
            return CategoryIntent.Cosmetics;

        if (q.Contains("مستلزمات") || q.Contains("مطهرات") || q.Contains("شاش") || q.Contains("قطن") || q.Contains("سرنجات") || q.Contains("supplies"))
            return CategoryIntent.Supplies;

        return CategoryIntent.None;
    }

    private static bool MatchesCategoryIntent(string name, string? generic, string? dosageForm, CategoryIntent intent)
    {
        if (intent == CategoryIntent.None) return true;

        return intent switch
        {
            CategoryIntent.Diabetes => IsDiabetesMedicine(name, generic),
            CategoryIntent.Hypertension => IsHypertensionMedicine(name, generic),
            CategoryIntent.Pediatric => IsPediatricMedicine(name, dosageForm),
            CategoryIntent.Antibiotic => IsAntibioticMedicine(name, generic),
            CategoryIntent.Painkiller => IsPainkillerMedicine(name, generic),
            CategoryIntent.Cosmetics => IsPersonalCareOrCosmetics(name, generic, dosageForm),
            CategoryIntent.Supplies => IsMedicalSupplies(name, generic, dosageForm),
            _ => true
        };
    }

    private static bool IsDiabetesMedicine(string name, string? generic)
    {
        var text = (name + " " + (generic ?? "")).ToLowerInvariant();
        return text.Contains("metformin") || text.Contains("cidophage") || text.Contains("amophage") ||
               text.Contains("diabenor") || text.Contains("glimepiride") || text.Contains("daonil") ||
               text.Contains("gliclazide") || text.Contains("diamicron") || text.Contains("insulin") ||
               text.Contains("إنسولين") || text.Contains("انسولين") || text.Contains("اموفاج") ||
               text.Contains("سيدوفاج") || text.Contains("ديابينور") || text.Contains("داونيل") ||
               text.Contains("insumed") || (text.Contains("syringe") && text.Contains("insulin"));
    }

    private static bool IsOralDiabetesMedicine(string name, string? generic)
    {
        var text = (name + " " + (generic ?? "")).ToLowerInvariant();
        return text.Contains("metformin") || text.Contains("cidophage") || text.Contains("amophage") ||
               text.Contains("diabenor") || text.Contains("glimepiride") || text.Contains("daonil") ||
               text.Contains("gliclazide") || text.Contains("diamicron") || text.Contains("سيدوفاج") ||
               text.Contains("اموفاج") || text.Contains("ديابينور");
    }

    private static bool IsInsulinOrSyringe(string name, string? generic)
    {
        var text = (name + " " + (generic ?? "")).ToLowerInvariant();
        return text.Contains("insulin") || text.Contains("إنسولين") || text.Contains("انسولين") ||
               text.Contains("insumed") || (text.Contains("syringe") && text.Contains("insulin"));
    }

    private static bool IsHypertensionMedicine(string name, string? generic)
    {
        var text = (name + " " + (generic ?? "")).ToLowerInvariant();
        return text.Contains("bisoprolol") || text.Contains("bisolock") || text.Contains("concor") ||
               text.Contains("sinopril") || text.Contains("enalapril") || text.Contains("atenolol") ||
               text.Contains("blokium") || text.Contains("aldactone") || text.Contains("diltiazem") ||
               text.Contains("delay tiazem") || text.Contains("rampecardin") || text.Contains("ramipril") ||
               text.Contains("olmeborg") || text.Contains("amlodipine") || text.Contains("exforge") ||
               text.Contains("capoten") || text.Contains("captopril") || text.Contains("ضغط") || text.Contains("قلب");
    }

    private static bool IsAntibioticMedicine(string name, string? generic)
    {
        var text = (name + " " + (generic ?? "")).ToLowerInvariant();
        return text.Contains("amox") || text.Contains("amoxil") || text.Contains("flucamox") ||
               text.Contains("cef") || text.Contains("curisafe") || text.Contains("cifin") ||
               text.Contains("cipro") || text.Contains("azithro") || text.Contains("augmentin") ||
               text.Contains("gentamicin") || text.Contains("epigent") || text.Contains("clindamycin") ||
               text.Contains("clindagram") || text.Contains("antibiotic") || text.Contains("مضاد حيوي");
    }

    private static bool IsPainkillerMedicine(string name, string? generic)
    {
        var text = (name + " " + (generic ?? "")).ToLowerInvariant();
        return text.Contains("diclofenac") || text.Contains("declophen") || text.Contains("epifenac") ||
               text.Contains("brufen") || text.Contains("ibuprofen") || text.Contains("paracetamol") ||
               text.Contains("ketofan") || text.Contains("ketoprek") || text.Contains("actifast") ||
               text.Contains("rheumarene") || text.Contains("adwiflam") || text.Contains("analgesic") ||
               text.Contains("myoflex") || text.Contains("cetafen") || text.Contains("cataflam") ||
               text.Contains("مسكن") || text.Contains("عظام") || text.Contains("روماتيزم");
    }

    private static (DateTime? StartDate, DateTime? EndDate) DetectTimeRangeFromQuestion(string question)
    {
        if (string.IsNullOrWhiteSpace(question)) return (null, null);
        var q = question.ToLowerInvariant();

        if (q.Contains("الشهر الماضي") || q.Contains("اخر شهر") || q.Contains("آخر شهر") || q.Contains("30 يوم") || q.Contains("شهر"))
        {
            return (DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);
        }

        if (q.Contains("الأسبوع الماضي") || q.Contains("الاسبوع الماضي") || q.Contains("هذا الأسبوع") || q.Contains("هذا الاسبوع") || q.Contains("7 أيام") || q.Contains("7 ايام") || q.Contains("أسبوع") || q.Contains("اسبوع"))
        {
            return (DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);
        }

        if (q.Contains("هذا العام") || q.Contains("السنة") || q.Contains("365 يوم") || q.Contains("سنة"))
        {
            return (DateTime.UtcNow.AddDays(-365), DateTime.UtcNow);
        }

        return (null, null);
    }
}

public enum CategoryIntent
{
    None,
    Diabetes,
    Hypertension,
    Pediatric,
    Antibiotic,
    Painkiller,
    Cosmetics,
    Supplies
}

public interface IPromptExecutionServiceOwner { }
