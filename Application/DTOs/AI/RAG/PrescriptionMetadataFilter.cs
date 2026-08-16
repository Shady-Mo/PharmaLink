namespace Application.DTOs.AI.RAG;

public class PrescriptionMetadataFilter
{
    public Guid? RestrictedBranchId { get; set; }
    public string? City { get; set; }
    public string? Governorate { get; set; }
    public bool? IsPediatric { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
