namespace Application.DTOs.Prescriptions;

using Domain.Entities;

public class PrescriptionResponseDto
{
    public Guid Id { get; set; }
    
    public string FileUrl { get; set; } = string.Empty;
    
    public PrescriptionStatus Status { get; set; }
    
    public DateTime UploadedAt { get; set; }
    
    public string? RejectionReason { get; set; }
}
