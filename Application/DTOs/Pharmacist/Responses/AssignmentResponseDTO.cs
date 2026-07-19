namespace Application.DTOs.Pharmacist.Responses;

public class AssignmentResponseDTO
{
    public Guid AssignmentId { get; set; }
    public Guid PharmacistId { get; set; }
    public Guid PharmacyId { get; set; }
    public DateTime AssignedAt { get; set; }
    public bool IsActive { get; set; }
    public string Message { get; set; }
}
