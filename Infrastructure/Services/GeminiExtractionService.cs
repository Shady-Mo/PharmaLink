using System.Net.Http.Json;

namespace Infrastructure.Services;

public class GeminiExtractionService(
    IHttpClientFactory httpClientFactory,
    IOptions<GeminiSettings> settings,
    ILogger<GeminiExtractionService> logger)
    : IAIExtractionService
{
    public const string HttpClientName = "GeminiClient";
    private readonly GeminiSettings _settings = settings.Value;

    public async Task<AIExtractionResult> ExtractMedicinesFromImageAsync(
        string absoluteImagePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var bytes = await File.ReadAllBytesAsync(absoluteImagePath, cancellationToken);
            var base64Data = Convert.ToBase64String(bytes);

            var ext = Path.GetExtension(absoluteImagePath).ToLowerInvariant();
            var mimeType = ext switch
            {
                ".png" => "image/png",
                ".webp" => "image/webp",
                ".heic" => "image/heic",
                _ => "image/jpeg"
            };

            const string promptText =
                "Extract all medicines from this prescription image. " +
                "Return ONLY a valid JSON object with a single property named 'medicines'. " +
                "Do not include markdown, explanations, or code fences. " +
                "Each medicine object must contain the following fields:\n" +
                "- medicineName (string, required)\n" +
                "- genericName (string or null)\n" +
                "- strength (string or null)\n" +
                "- dosageForm (string or null)\n" +
                "- dose (string or null)\n" +
                "- frequency (string or null)\n" +
                "- duration (string or null)\n" +
                "- quantity (integer, never null)\n" +
                "- route (string or null)\n" +
                "- confidence (number between 0.0 and 1.0, never null)\n\n" +
                "Rules:\n" +
                "1. medicineName is mandatory.\n" +
                "2. If any string field cannot be determined, return null.\n" +
                "3. If quantity cannot be determined, return 1.\n" +
                "4. confidence must always be a number between 0.0 and 1.0. If uncertain, use a lower value such as 0.5.\n" +
                "5. Do not omit any field.\n" +
                "6. Return only valid JSON.\n\n" +
                "The JSON must have this structure:\n" +
                "{\n" +
                "  \"medicines\": [\n" +
                "    {\n" +
                "      \"medicineName\": \"string\",\n" +
                "      \"genericName\": null,\n" +
                "      \"strength\": null,\n" +
                "      \"dosageForm\": null,\n" +
                "      \"dose\": null,\n" +
                "      \"frequency\": null,\n" +
                "      \"duration\": null,\n" +
                "      \"quantity\": 1,\n" +
                "      \"route\": null,\n" +
                "      \"confidence\": 0.95\n" +
                "    }\n" +
                "  ]\n" +
                "}";

            var requestPayload = new GeminiRequest
            {
                Contents =
                [
                    new GeminiContent
                    {
                        Parts =
                        [
                            new GeminiPartText { Text = promptText },
                            new GeminiPartInlineData
                            {
                                InlineData = new GeminiInlineData
                                {
                                    MimeType = mimeType,
                                    Data = base64Data
                                }
                            }
                        ]
                    }
                ],
                GenerationConfig = new GeminiGenerationConfig
                {
                    ResponseMimeType = "application/json"
                }
            };

            var httpClient = httpClientFactory.CreateClient(HttpClientName);
            var url = $"{_settings.Endpoint.TrimEnd('/')}/{_settings.ModelName}:generateContent?key={_settings.ApiKey}";

            logger.LogInformation("Calling Gemini API with model {Model}", _settings.ModelName);
            var response = await httpClient.PostAsJsonAsync(url, requestPayload, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogError("Gemini API call failed with status code {StatusCode}. Content: {Content}",
                    response.StatusCode, errorContent);
                return new AIExtractionResult();
            }

            var geminiResponse =
                await response.Content.ReadFromJsonAsync<GeminiResponse>(cancellationToken: cancellationToken);
            var text = geminiResponse?.Candidates?[0]?.Content?.Parts?[0]?.Text;

            if (string.IsNullOrWhiteSpace(text))
            {
                logger.LogWarning("Gemini API returned an empty text content.");
                return new AIExtractionResult();
            }

            logger.LogDebug("Gemini raw response text: {Text}", text);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var extractedResult = JsonSerializer.Deserialize<GeminiExtractedJson>(text, options);

            if (extractedResult?.Medicines == null)
            {
                logger.LogWarning("Deserialized Gemini medicines array is null.");
                return new AIExtractionResult();
            }

            var medicinesList = new List<ExtractedMedicineItem>();
            foreach (var item in extractedResult.Medicines)
            {
                if (string.IsNullOrWhiteSpace(item.MedicineName))
                    continue;

                medicinesList.Add(new ExtractedMedicineItem
                {
                    MedicineName = item.MedicineName,
                    GenericName = item.GenericName,
                    Strength = item.Strength,
                    DosageForm = item.DosageForm,
                    Dose = item.Dose,
                    Frequency = item.Frequency,
                    Duration = item.Duration,
                    Quantity = item.Quantity.GetValueOrDefault(1) > 0
                        ? item.Quantity.Value
                        : 1,
                    Route = item.Route,
                    Confidence = item.Confidence
                });
            }

            return new AIExtractionResult
            {
                ModelUsed = _settings.ModelName,
                Medicines = medicinesList
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception occurred during Gemini AI medicine extraction.");
            return new AIExtractionResult();
        }
    }

    #region Gemini API Helper Contracts

    private class GeminiRequest
    {
        [JsonPropertyName("contents")] public List<GeminiContent> Contents { get; set; } = [];

        [JsonPropertyName("generationConfig")] public GeminiGenerationConfig GenerationConfig { get; set; } = new();
    }

    private class GeminiContent
    {
        [JsonPropertyName("parts")] public List<object> Parts { get; set; } = [];
    }

    private class GeminiPartText
    {
        [JsonPropertyName("text")] public string Text { get; set; } = string.Empty;
    }

    private class GeminiPartInlineData
    {
        [JsonPropertyName("inlineData")] public GeminiInlineData InlineData { get; set; } = new();
    }

    private class GeminiInlineData
    {
        [JsonPropertyName("mimeType")] public string MimeType { get; set; } = string.Empty;

        [JsonPropertyName("data")] public string Data { get; set; } = string.Empty;
    }

    private class GeminiGenerationConfig
    {
        [JsonPropertyName("responseMimeType")] public string ResponseMimeType { get; set; } = "application/json";
        [JsonPropertyName("thinkingConfig")] public GeminiThinkingConfig Thinking { get; set; } = new();
    }

    private class GeminiThinkingConfig
    {
        [JsonPropertyName("thinkingLevel")] public string ThinkingLevel { get; set; } = "MEDIUM";
    }

    private class GeminiResponse
    {
        [JsonPropertyName("candidates")] public List<GeminiCandidate>? Candidates { get; set; }
    }

    private class GeminiCandidate
    {
        [JsonPropertyName("content")] public GeminiResponseContent? Content { get; set; }
    }

    private class GeminiResponseContent
    {
        [JsonPropertyName("parts")] public List<GeminiResponsePart>? Parts { get; set; }
    }

    private class GeminiResponsePart
    {
        [JsonPropertyName("text")] public string? Text { get; set; }
    }

    private class GeminiExtractedJson
    {
        [JsonPropertyName("medicines")] public List<GeminiExtractedMedicineItem>? Medicines { get; set; }
    }

    private class GeminiExtractedMedicineItem
    {
        public string MedicineName { get; set; } = string.Empty;
        public string? GenericName { get; set; }
        public string? Strength { get; set; }
        public string? DosageForm { get; set; }
        public string? Dose { get; set; }
        public string? Frequency { get; set; }
        public string? Duration { get; set; }

        public int? Quantity { get; set; }

        public string? Route { get; set; }

        public double? Confidence { get; set; }
    }

    #endregion
}