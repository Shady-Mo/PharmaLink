using Application.Services.AI.Models;

namespace Infrastructure.AI.Validation;

public class PrescriptionExtractionBusinessValidator : IAIResponseValidator<AIExtractionResult>
{
    public Application.Services.AI.Models.ValidationResult Validate(AIExtractionResult response)
    {
        var result = new Application.Services.AI.Models.ValidationResult();

        if (!response.IsValidPrescription)
        {
            return result;
        }

        if (response.Medicines.Count == 0)
        {
            result.Errors.Add("A valid prescription must contain at least one medicine.");
        }

        foreach (var medicine in response.Medicines)
        {
            if (string.IsNullOrWhiteSpace(medicine.MedicineName))
            {
                result.Errors.Add("Medicine name is required.");
            }

            if (medicine.Quantity <= 0)
            {
                result.Errors.Add($"Medicine '{medicine.MedicineName}' has an invalid quantity.");
            }

            if (medicine.Confidence is < 0 or > 1)
            {
                result.Errors.Add($"Medicine '{medicine.MedicineName}' has an invalid confidence score.");
            }
        }

        return result;
    }
}
