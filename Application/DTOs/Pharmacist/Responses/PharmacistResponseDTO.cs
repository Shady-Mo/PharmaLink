namespace Application.DTOs.Pharmacist.Responses;

public class PharmacistResponseDTO
{
    public Guid PharmacistId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PharmacyLegalName { get; set; } = string.Empty;
    public Guid BranchId { set; get; }
    public string? BranchName { get; set; }
    public string? BranchCity { get; set; }
    public string? BranchAddress { get; set; }
    public string? BranchPhone { get; set; }
    public DateTime CreatedAt { get; set; }
}
