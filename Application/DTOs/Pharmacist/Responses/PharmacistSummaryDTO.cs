namespace Application.DTOs.Pharmacist.Responses;

public class PharmacistSummaryDTO
{
    public Guid PharmacistId { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
}
