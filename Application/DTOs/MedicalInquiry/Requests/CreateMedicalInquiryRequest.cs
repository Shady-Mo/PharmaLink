using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.MedicalInquiry.Requests;

public class CreateMedicalInquiryRequest
{
    [Required]
    [MaxLength(2000)]
    public string Question { get; set; } = string.Empty;
}
