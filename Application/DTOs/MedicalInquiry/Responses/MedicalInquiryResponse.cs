namespace Application.DTOs.MedicalInquiry.Responses;

public class MedicalInquiryResponse
{
    public Guid MedicalInquiryId { get; set; }
    public Guid PatientUserId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public string? Answer { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? AnsweredByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? AnsweredAt { get; set; }
}
