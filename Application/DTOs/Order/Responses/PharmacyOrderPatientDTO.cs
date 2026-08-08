namespace Application.DTOs.Order.Responses;

public class PharmacyOrderPatientDTO
{
    public Guid PatientUserId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }
}
