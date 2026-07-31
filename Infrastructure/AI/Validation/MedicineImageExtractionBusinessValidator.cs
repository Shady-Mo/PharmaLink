using Application.DTOs.AI;

namespace Infrastructure.AI.Validation;

public class MedicineImageExtractionBusinessValidator : IAIResponseValidator<MedicineImageExtractionResponseDTO>
{
    public Application.Services.AI.Models.ValidationResult Validate(MedicineImageExtractionResponseDTO response)
    {
        var result = new Application.Services.AI.Models.ValidationResult();

        if (string.IsNullOrWhiteSpace(response.MedicineName))
        {
            result.Errors.Add("Medicine name is required.");
        }

        if (response.Confidence is < 0 or > 1)
        {
            result.Errors.Add("Confidence must be between 0 and 1.");
        }

        return result;
    }
}
