namespace Application.DTOs.PrescriptionReview.Requests;

public class AddPrescriptionReviewMedicinesToCartDTO
{
    public List<Guid> PrescriptionReviewMedicineIds { get; set; } = [];
}
