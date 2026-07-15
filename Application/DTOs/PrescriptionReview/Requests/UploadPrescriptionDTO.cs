using Microsoft.AspNetCore.Http;

namespace Application.DTOs.PrescriptionReview.Requests;

public class UploadPrescriptionDTO
{
    public IFormFile Image { get; set; } = null!;
}
