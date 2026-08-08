namespace Application.DTOs.Prescriptions;

using Microsoft.AspNetCore.Http;

public class UploadPrescriptionRequest
{
    public IFormFile File { get; set; } = null!;
}
