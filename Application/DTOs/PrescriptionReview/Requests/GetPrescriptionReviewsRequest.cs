using Domain.Enums;

namespace Application.DTOs.PrescriptionReview.Requests;

public class GetPrescriptionReviewsRequest : PaginatedRequest
{
    public PrescriptionReviewStatus? Status { get; set; }
    public string? SearchTerm { get; set; }
    public string? SortBy { get; set; }
    public bool IsDescending { get; set; } = false;
}
