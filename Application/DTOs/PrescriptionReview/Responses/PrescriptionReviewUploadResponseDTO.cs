using System;
using System.Collections.Generic;

namespace Application.DTOs.PrescriptionReview.Responses;

public class PrescriptionReviewUploadResponseDTO
{
    public Guid ReviewId { get; set; }
    public Guid PrescriptionReviewId
    {
        get => ReviewId;
        set => ReviewId = value;
    }

    public Guid? CartId { get; set; }
    public string ProcessingStatus { get; set; } = "PendingPharmacistReview";
    public string Status { get; set; } = "PendingReview";
    public string ImageUrl { get; set; } = string.Empty;
    public List<ExtractedMedicineSummaryDTO> Medicines { get; set; } = [];
    public List<ExtractedMedicineSummaryDTO> ExtractedItems
    {
        get => Medicines;
        set => Medicines = value;
    }
}
