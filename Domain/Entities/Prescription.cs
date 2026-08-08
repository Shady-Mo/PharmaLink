namespace Domain.Entities;

public class Prescription
{
    public Guid Id { get; set; }
    
    public Guid PatientId { get; set; }
    
    public Guid? OrderId { get; set; }
    
    public PrescriptionStatus Status { get; set; }
    
    public string FileName { get; set; } = string.Empty;
    
    public string FileUrl { get; set; } = string.Empty;
    
    public string StoragePath { get; set; } = string.Empty;
    
    public string ContentType { get; set; } = string.Empty;
    
    public long FileSize { get; set; }
    
    public DateTime UploadedAt { get; set; }
    
    public DateTime? ConsumedAt { get; set; }
    
    public string? RejectionReason { get; set; }
    
    // Navigation properties
    public Patient Patient { get; set; } = null!;
    public Order? Order { get; set; }
}
