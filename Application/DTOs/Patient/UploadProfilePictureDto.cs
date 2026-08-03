using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Patient;

public class UploadProfilePictureDto
{
    [Required(ErrorMessage = "Please select an image to upload.")]
    public IFormFile Image { get; set; } = null!;
}
