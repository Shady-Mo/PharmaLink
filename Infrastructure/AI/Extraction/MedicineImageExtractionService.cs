using Application.DTOs.AI;
using Application.Services.AI.Models;
using Infrastructure.AI.Validation;

namespace Infrastructure.AI.Extraction;

public class MedicineImageExtractionService(
    IPromptExecutionService promptExecutionService,
    IAIResponseValidator<MedicineImageExtractionResponseDTO> businessValidator,
    ILogger<MedicineImageExtractionService> logger)
    : IMedicineImageExtractionService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<MedicineImageExtractionResponseDTO> ExtractAsync(
        AIFileContent file,
        CancellationToken cancellationToken = default)
    {
        var execution = await promptExecutionService.ExecuteAsync(
            new PromptExecutionRequest
            {
                PromptName = PromptNames.ExtractMedicineImage,
                PromptVersion = "v1",
                TaskType = AITaskType.Vision,
                File = file
            },
            cancellationToken);

        var json = AIJson.ExtractJsonObject(execution.RawResponse);
        MedicineImageExtractionResponseDTO? response = null;
        try
        {
            response = JsonSerializer.Deserialize<MedicineImageExtractionResponseDTO>(json, JsonOptions);
        }
        catch (System.Text.Json.JsonException ex)
        {
            logger.LogWarning(ex, "Failed to parse AI response as JSON: {RawResponse}", execution.RawResponse);
        }

        response ??= new MedicineImageExtractionResponseDTO();

        var validation = businessValidator.Validate(response);
        if (!validation.IsValid)
        {
            logger.LogWarning(
                "Medicine image extraction business validation failed: {Errors}",
                string.Join("; ", validation.Errors));
        }

        return response;
    }
}
