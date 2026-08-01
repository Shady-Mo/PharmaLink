namespace Application.DTOs.Pharmacist.Requests
{
    public class GetAllPharmacistsRequestDTO : PaginatedRequest
    {
        public string? Search { set; get; }

        public Guid? BranchId { set; get; }

        public UserStatus? userStatus { set; get; }
    }
}
