using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.MedicalInquiry.Requests;

public class AnswerMedicalInquiryRequest
{
    [Required]
    [MaxLength(4000)]
    public string Answer { get; set; } = string.Empty;
}
