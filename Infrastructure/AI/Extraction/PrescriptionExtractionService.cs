using Application.Services.AI.Models;
using Infrastructure.AI.Validation;

namespace Infrastructure.AI.Extraction;

public class PrescriptionExtractionService(
    IPromptExecutionService promptExecutionService,
    IAIResponseValidator<AIExtractionResult> businessValidator,
    ILogger<PrescriptionExtractionService> logger)
    : IPrescriptionExtractionService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<AIExtractionResult> ExtractAsync(
        AIFileContent file,
        CancellationToken cancellationToken = default)
    {
        var execution = await promptExecutionService.ExecuteAsync(
            new PromptExecutionRequest
            {
                PromptName = PromptNames.ExtractPrescription,
                PromptVersion = "v1",
                TaskType = AITaskType.Vision,
                File = file
            },
            cancellationToken);

        var json = AIJson.ExtractJsonObject(execution.RawResponse);
        AIExtractionResult? response = null;
        try
        {
            response = JsonSerializer.Deserialize<AIExtractionResult>(json, JsonOptions);
        }
        catch (System.Text.Json.JsonException ex)
        {
            logger.LogWarning(ex, "Failed to parse AI response as JSON: {RawResponse}", execution.RawResponse);
        }

        response ??= new AIExtractionResult
        {
            IsValidPrescription = false,
            ValidationMessage = "AI returned an invalid or empty response.",
            ExtractedText = execution.RawResponse
        };

        response.ModelUsed = string.IsNullOrWhiteSpace(execution.ModelId)
            ? execution.Provider
            : execution.ModelId;
        response.RawResponse = execution.RawResponse;

        var validation = businessValidator.Validate(response);
        if (!validation.IsValid)
        {
            logger.LogWarning(
                "Prescription extraction business validation failed: {Errors}",
                string.Join("; ", validation.Errors));

            response.IsValidPrescription = false;
            response.ValidationMessage = string.Join(" ", validation.Errors);
        }

        return response;
    }
}
