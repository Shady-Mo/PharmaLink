using System;
using System.Collections.Generic;

namespace Application.DTOs.PrescriptionReview.Responses;

public class PrescriptionReviewUploadResponseDTO
{
    public Guid ReviewId { get; set; }
    public string Status { get; set; } = "PendingReview";
    public string ImageUrl { get; set; } = string.Empty;
    public List<ExtractedMedicineSummaryDTO> Medicines { get; set; } = [];
}
