namespace Application.DTOs.PharmacyBranch.Response;

public class GetPharmacyBranchResponseDTO {
    public Guid BranchId { get; set; }
    public Guid PharmacyId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public bool SupportsDelivery { get; set; }
    public bool SupportsPickup { get; set; }
}
