using System;

namespace Application.DTOs.PrescriptionReview.Responses;

public class PrescriptionReviewSummaryDTO
{
    public Guid ReviewId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int MedicineCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
}
