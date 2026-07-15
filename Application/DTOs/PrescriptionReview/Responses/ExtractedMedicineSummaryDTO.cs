using System;

namespace Application.DTOs.PrescriptionReview.Responses;

public class ExtractedMedicineSummaryDTO
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Strength { get; set; }
    public string? DosageForm { get; set; }
    public string? Frequency { get; set; }
    public string? Duration { get; set; }
    public int Quantity { get; set; }
    public double? Confidence { get; set; }
}
