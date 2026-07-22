using System;
using Domain.Enums;

namespace Application.DTOs.PharmacyOwner.Request
{
    public class GetPharmacyOwnersRequest : PaginatedRequest
    {
        public string? Search { get; set; }
        public UserStatus? Status { get; set; }
        public Guid? PharmacyId { get; set; }
    }
}
