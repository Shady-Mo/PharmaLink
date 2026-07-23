namespace Application.DTOs.PharmacyBranch.Response;

public class PharmacyBranchSearchDTO {
    public Guid BranchId { get; set; }
    public Guid PharmacyId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
}
