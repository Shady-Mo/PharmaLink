using Domain.Enums;

namespace Application.DTOs.PrescriptionReview.Requests;

public class GetPrescriptionReviewsRequest : PaginatedRequest
{
    public PrescriptionReviewStatus? Status { get; set; }
}
