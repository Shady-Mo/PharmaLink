namespace Application.DTOs.AI;

public sealed class PrescriptionAnalyticsQuestionRequest
{
    public required string Question { get; init; }
}

public sealed class PrescribedDrugMetricDTO
{
    public required string MedicineName { get; init; }
    public string? Category { get; init; }
    public int MentionCount { get; init; }
    public int TotalQuantity { get; init; }
    public double Percentage { get; init; }
}

public sealed class CategoryMetricDTO
{
    public required string CategoryName { get; init; }
    public int Count { get; init; }
    public double Percentage { get; init; }
    public string? ColorHint { get; init; }
}

public sealed class PrescriptionAnalyticsAnswerResponse
{
    public required string Answer { get; init; }
    public required IReadOnlyList<PrescriptionAnalyticsSourceDTO> Sources { get; init; }
    public bool HasMatches => Sources.Count > 0;
    public int TotalPrescriptionsAnalyzed { get; init; }
    public IReadOnlyList<PrescribedDrugMetricDTO> TopPrescribedDrugs { get; init; } = [];
    public IReadOnlyList<CategoryMetricDTO> MostRequestedCategories { get; init; } = [];
}

public sealed class PrescriptionAnalyticsSourceDTO
{
    public required Guid PrescriptionId { get; init; }
    public string? DoctorName { get; init; }
    public string? Specialty { get; init; }
    public string? ClinicOrHospital { get; init; }
    public DateTime VisitDate { get; init; }
    public string? DiagnosisNotes { get; init; }
    public string? PatientAddress { get; init; }
    public string ImageUrl { get; init; } = string.Empty;
    public double RelevanceScore { get; init; }
    public required IReadOnlyList<PrescriptionAnalyticsMedicineDTO> Medicines { get; init; }
}

public sealed class PrescriptionAnalyticsMedicineDTO
{
    public required Guid PrescriptionReviewMedicineId { get; init; }
    public string MedicineName { get; init; } = string.Empty;
    public string? Strength { get; init; }
    public string? DosageForm { get; init; }
    public string? Dose { get; init; }
    public string? Frequency { get; init; }
    public int Quantity { get; init; }
    public Guid? MatchedDrugId { get; init; }
    public Guid? SuggestedAlternativeDrugId { get; init; }
    public bool CanBeAddedToCart { get; init; }
}
