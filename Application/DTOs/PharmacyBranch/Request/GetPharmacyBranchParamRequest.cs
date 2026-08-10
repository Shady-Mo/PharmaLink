namespace Application.DTOs.PharmacyBranch.Request;
public class GetPharmacyBranchParamRequest : PaginatedRequest
{
    public string? Search { get; set; }
}
