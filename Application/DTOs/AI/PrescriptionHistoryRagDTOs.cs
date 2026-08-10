namespace Application.DTOs.AI;

public sealed class PrescriptionHistoryQuestionRequest
{
    public required string Question { get; init; }
}

public sealed class PrescriptionHistoryAnswerResponse
{
    public required string Answer { get; init; }
    public required IReadOnlyList<PrescriptionHistorySourceDTO> Sources { get; init; }
    public bool HasMatches => Sources.Count > 0;
}

public sealed class PrescriptionHistorySourceDTO
{
    public required Guid PrescriptionId { get; init; }
    public string? DoctorName { get; init; }
    public string? Specialty { get; init; }
    public string? ClinicOrHospital { get; init; }
    public DateTime VisitDate { get; init; }
    public string? DiagnosisNotes { get; init; }
    public string ImageUrl { get; init; } = string.Empty;
    public double RelevanceScore { get; init; }
    public required IReadOnlyList<PrescriptionHistoryMedicineDTO> Medicines { get; init; }
}

public sealed class PrescriptionHistoryMedicineDTO
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
