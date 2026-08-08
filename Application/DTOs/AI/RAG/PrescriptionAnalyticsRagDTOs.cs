namespace Application.DTOs.AI.RAG;

/// <summary>
/// Request body for the Prescription Analytics RAG endpoint.
/// topK, minSimilarity, and isPediatric are determined automatically by the system.
/// </summary>
public class PrescriptionAnalyticsRagRequestDTO
{
    /// <summary>Natural language question in Arabic or English.</summary>
    public string Question { get; set; } = string.Empty;

    /// <summary>
    /// Optional: Filter to a specific pharmacy branch.
    /// If provided, the system verifies the caller owns/works at this branch.
    /// Competitor branch IDs are rejected with 403.
    /// </summary>
    public Guid? BranchId { get; set; }

    /// <summary>Optional: Filter prescriptions by patient city (market-wide, not branch-restricted).</summary>
    public string? City { get; set; }

    /// <summary>Optional: Filter prescriptions by patient governorate.</summary>
    public string? Governorate { get; set; }

    /// <summary>Optional: Start of date range for prescription analysis.</summary>
    public DateTime? StartDate { get; set; }

    /// <summary>Optional: End of date range for prescription analysis.</summary>
    public DateTime? EndDate { get; set; }
}

/// <summary>
/// Enriched response DTO — all fields are frontend-ready for Angular components, charts, and cards.
/// </summary>
public class PrescriptionAnalyticsRagResponseDTO
{
    /// <summary>Full AI-generated answer from Dr. Ziad, focused on the specific question asked.</summary>
    public string Answer { get; set; } = string.Empty;

    /// <summary>Total number of prescriptions found and analyzed.</summary>
    public int TotalPrescriptionsAnalyzed { get; set; }

    /// <summary>Ranked list of most prescribed drugs with counts, quantities, and percentages.</summary>
    public List<PrescribedDrugMetricDTO> TopPrescribedDrugs { get; set; } = [];

    /// <summary>Drug category breakdown (pediatric, antibiotics, chronic, etc.) with percentages.</summary>
    public List<CategoryMetricDTO> MostRequestedCategories { get; set; } = [];

    /// <summary>Shortage warnings for high-demand drugs vs available stock. Empty if no branchId scope.</summary>
    public List<ShortageWarningDTO> ShortageWarnings { get; set; } = [];

    /// <summary>Short textual demand forecasting insights for the top drugs. Suitable for dashboard alerts.</summary>
    public List<string> DemandForecastingInsights { get; set; } = [];

    /// <summary>Lightweight references to matched prescriptions (for linking in UI).</summary>
    public List<PrescriptionRefDTO> MatchedPrescriptions { get; set; } = [];

    /// <summary>Scope metadata shown in UI (e.g., "مدينة القاهرة", "جميع المدن").</summary>
    public string AnalysisScope { get; set; } = string.Empty;

    /// <summary>Time range displayed in UI (e.g., "من 2026-07-01 إلى 2026-08-08").</summary>
    public string AnalysisTimeRange { get; set; } = string.Empty;

    /// <summary>AI provider used (e.g., "Gemini:gemini-2.5-flash") or "NoData" / "FallbackEngine".</summary>
    public string UsedProvider { get; set; } = string.Empty;

    /// <summary>Total execution time in milliseconds — useful for UI loading feedback.</summary>
    public long ExecutionTimeMs { get; set; }
}

/// <summary>Drug-level metric with all fields needed for charts and ranked lists in Angular.</summary>
public class PrescribedDrugMetricDTO
{
    public string MedicineName { get; set; } = string.Empty;
    public string? GenericName { get; set; }

    /// <summary>Number of prescriptions containing this drug.</summary>
    public int MentionCount { get; set; }

    /// <summary>Total quantity units across all prescriptions.</summary>
    public int TotalQuantity { get; set; }

    /// <summary>Percentage of total analyzed prescriptions that include this drug.</summary>
    public double Percentage { get; set; }

    /// <summary>Whether this drug is flagged as pediatric.</summary>
    public bool IsPediatric { get; set; }

    /// <summary>Trend indicator: "up", "stable", or "down" — placeholder for future time-series comparison.</summary>
    public string Trend { get; set; } = "stable";
}

/// <summary>Category-level aggregation for pie charts and category breakdowns.</summary>
public class CategoryMetricDTO
{
    public string CategoryName { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }

    /// <summary>Display color hint for Angular charts (e.g., "#4CAF50").</summary>
    public string ColorHint { get; set; } = "#607D8B";
}

/// <summary>Shortage warning for a specific drug — maps to colored alert cards in Angular.</summary>
public class ShortageWarningDTO
{
    public string DrugName { get; set; } = string.Empty;

    /// <summary>Human-readable reason why this drug is flagged (e.g., high demand count).</summary>
    public string HighDemandReason { get; set; } = string.Empty;

    /// <summary>Current stock count in the branch inventory (-1 if inventory not queried).</summary>
    public int AvailableStock { get; set; } = -1;

    /// <summary>Recommended restocking action.</summary>
    public string Recommendation { get; set; } = string.Empty;

    /// <summary>
    /// Urgency level for card color coding in Angular:
    /// "Critical" (red) = stock 0, "High" (orange) = stock below demand, "Medium" (yellow) = low buffer.
    /// </summary>
    public string UrgencyLevel { get; set; } = "Medium";
}

/// <summary>Lightweight prescription reference for drilling down in UI.</summary>
public class PrescriptionRefDTO
{
    public Guid PrescriptionReviewId { get; set; }
    public string City { get; set; } = string.Empty;
    public string Governorate { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public double SimilarityScore { get; set; }
}

/// <summary>Internal metadata filter passed to the vector store SQL query.</summary>
public class PrescriptionMetadataFilter
{
    /// <summary>
    /// When set, restricts search to a single branch only.
    /// null = search all prescriptions (market-wide query).
    /// </summary>
    public Guid? RestrictedBranchId { get; set; }

    /// <summary>Filter by patient city (market data — not branch restricted).</summary>
    public string? City { get; set; }

    /// <summary>Filter by patient governorate.</summary>
    public string? Governorate { get; set; }

    /// <summary>When true, restricts to pediatric prescriptions only.</summary>
    public bool? IsPediatric { get; set; }
}
