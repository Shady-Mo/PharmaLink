namespace Application.DTOs.Pharmacist.Responses;

public class PharmacistResponseDTO
{
    public Guid PharmacistId { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public string Status { get; set; }
    public string PharmacyLegalName { get; set; }
    public DateTime CreatedAt { get; set; }
}
