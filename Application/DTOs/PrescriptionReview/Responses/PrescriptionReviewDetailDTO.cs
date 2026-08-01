using System;
using System.Collections.Generic;

namespace Application.DTOs.PrescriptionReview.Responses;

public class PrescriptionReviewDetailDTO
{
    public Guid ReviewId { get; set; }
    public Guid PatientUserId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ProcessingStatus { get; set; } = string.Empty;
    public string AIModel { get; set; } = string.Empty;
    public string? ReviewNotes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public Guid? CreatedOrderId { get; set; }
    public List<MedicineDetailDTO> Medicines { get; set; } = [];
}
