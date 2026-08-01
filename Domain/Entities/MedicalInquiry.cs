namespace Domain.Entities;

using Domain.Enums;

public class MedicalInquiry
{
    public Guid MedicalInquiryId { get; set; }

    public Guid PatientUserId { get; set; }

    public string Question { get; set; } = string.Empty;

    public string? Answer { get; set; }

    public MedicalInquiryStatus Status { get; set; } = MedicalInquiryStatus.Pending;

    public Guid? AnsweredByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? AnsweredAt { get; set; }

    public Patient Patient { get; set; } = null!;

    public AppUser? AnsweredBy { get; set; }
}
