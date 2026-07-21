using Domain.Enums;

namespace Application.DTOs.Pharmacy.Request
{
    public class GetAdminPharmaciesRequest : PaginatedRequest
    {
        public string? Search { get; set; }
        public VerificationStatus? Status { get; set; }
        public string? City { get; set; }
    }
}
