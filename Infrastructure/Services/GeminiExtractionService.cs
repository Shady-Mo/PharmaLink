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
                ".pdf" => "application/pdf",
                _ => "image/jpeg"
            };

            const string promptText =
                "You are an expert pharmacist and a prescription audit extraction engine for a pharmacy system. " +
                "First decide whether the uploaded image or PDF is a valid medical prescription. " +
                "Support handwritten, printed, and PDF prescriptions. " +
                "Reject invoices, product photos, insurance cards, lab reports, empty pages, and unrelated documents. " +
                "If it is not a prescription, return isValidPrescription=false, a validationMessage, and an empty medicines array. " +
                "If it is a prescription, extract all medicines. " +
                "CRITICAL INSTRUCTION FOR MEDICINE NAMES: Doctors often have poor handwriting or misspell drug names. " +
                "You MUST act as a pharmacist: carefully review the extracted name, correct any spelling mistakes, and output the standard, correct commercial brand name of the drug in the 'medicineName' field. " +
                "For example, if the handwriting looks like 'Pandl' or 'Brufn', you MUST correct it to 'Panadol' and 'Brufen' respectively. Do not just blindly transcribe typos. Standardizing the name is required for database searching. " +
                "Return ONLY a valid JSON object that follows the schema below. " +
                "Do not include markdown, explanations, or code fences. " +
                "The root object must contain:\n" +
                "- isValidPrescription (boolean, required)\n" +
                "- validationMessage (string or null)\n" +
                "- extractedText (string or null)\n" +
                "- aiSummary (string or null)\n" +
                "- extractionConfidence (number between 0.0 and 1.0)\n" +
                "- medicines (array, never null)\n\n" +
                "Each medicine object must contain the following fields:\n" +
                "- originalMedicineName (string, required): The EXACT literal text written in the prescription (before correction).\n" +
                "- medicineName (string, required): The CORRECTED commercial/brand name of the drug.\n" +
                "- genericName (string or null): The active ingredient(s) of the drug.\n" +
                "- strength (string or null)\n" +
                "- dosageForm (string or null)\n" +
                "- dose (string or null)\n" +
                "- frequency (string or null)\n" +
                "- duration (string or null)\n" +
                "- quantity (integer, never null)\n" +
                "- route (string or null)\n" +
                "- confidence (number between 0.0 and 1.0, never null)\n\n" +
                "Rules:\n" +
                "1. medicineName is mandatory for medicine rows only.\n" +
                "2. If any string field cannot be determined, return null.\n" +
                "3. If quantity cannot be determined, return 1.\n" +
                "4. confidence must always be a number between 0.0 and 1.0. If uncertain, use a lower value such as 0.5.\n" +
                "5. Do not omit any field.\n" +
                "6. If the document is not a prescription, medicines must be an empty array.\n" +
                "6. Return only valid JSON.\n\n" +
                "The JSON must have this structure:\n" +
                "{\n" +
                "  \"isValidPrescription\": true,\n" +
                "  \"validationMessage\": null,\n" +
                "  \"extractedText\": \"string\",\n" +
                "  \"aiSummary\": \"string\",\n" +
                "  \"extractionConfidence\": 0.95,\n" +
                "  \"medicines\": [\n" +
                "    {\n" +
                "      \"originalMedicineName\": \"string\",\n" +
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
            var parts = geminiResponse?.Candidates?[0]?.Content?.Parts;
            var text = parts != null ? string.Join("\n", parts.Select(p => p.Text)) : null;

            if (string.IsNullOrWhiteSpace(text))
            {
                logger.LogWarning("Gemini API returned an empty text content.");
                return new AIExtractionResult();
            }

            logger.LogDebug("Gemini raw response text: {Text}", text);
            
            var jsonText = text.Trim();
            if (jsonText.StartsWith("```"))
            {
                var firstNewline = jsonText.IndexOf('\n');
                if (firstNewline != -1)
                {
                    jsonText = jsonText.Substring(firstNewline + 1);
                }
                if (jsonText.EndsWith("```"))
                {
                    jsonText = jsonText.Substring(0, jsonText.Length - 3).Trim();
                }
            }
            
            var startIndex = jsonText.IndexOf('{');
            var endIndex = jsonText.LastIndexOf('}');
            if (startIndex >= 0 && endIndex >= startIndex)
            {
                jsonText = jsonText.Substring(startIndex, endIndex - startIndex + 1);
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            GeminiExtractedJson? extractedResult = null;
            try
            {
                extractedResult = JsonSerializer.Deserialize<GeminiExtractedJson>(jsonText, options);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to deserialize Gemini response. Cleaned text: {JsonText}", jsonText);
            }

            if (extractedResult?.Medicines == null)
            {
                logger.LogWarning("Deserialized Gemini medicines array is null.");
                return new AIExtractionResult();
            }

            if (!extractedResult.IsValidPrescription)
            {
                return new AIExtractionResult
                {
                    ModelUsed = _settings.ModelName,
                    IsValidPrescription = false,
                    ValidationMessage = extractedResult.ValidationMessage,
                    ExtractedText = extractedResult.ExtractedText,
                    AISummary = extractedResult.AISummary,
                    ExtractionConfidence = extractedResult.ExtractionConfidence,
                    Medicines = []
                };
            }

            var medicinesList = new List<ExtractedMedicineItem>();
            foreach (var item in extractedResult.Medicines)
            {
                if (string.IsNullOrWhiteSpace(item.MedicineName))
                    continue;

                var quantity = item.Quantity.GetValueOrDefault(1);

                medicinesList.Add(new ExtractedMedicineItem
                {
                    OriginalMedicineName = item.OriginalMedicineName,
                    MedicineName = item.MedicineName,
                    GenericName = item.GenericName,
                    Strength = item.Strength,
                    DosageForm = item.DosageForm,
                    Dose = item.Dose,
                    Frequency = item.Frequency,
                    Duration = item.Duration,
                    Quantity = quantity > 0 ? quantity : 1,
                    Route = item.Route,
                    Confidence = item.Confidence
                });
            }

            return new AIExtractionResult
            {
                ModelUsed = _settings.ModelName,
                IsValidPrescription = true,
                ValidationMessage = extractedResult.ValidationMessage,
                ExtractedText = extractedResult.ExtractedText,
                AISummary = extractedResult.AISummary,
                ExtractionConfidence = extractedResult.ExtractionConfidence,
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
        [JsonPropertyName("isValidPrescription")] public bool IsValidPrescription { get; set; }
        [JsonPropertyName("validationMessage")] public string? ValidationMessage { get; set; }
        [JsonPropertyName("extractedText")] public string? ExtractedText { get; set; }
        [JsonPropertyName("aiSummary")] public string? AISummary { get; set; }
        [JsonPropertyName("extractionConfidence")] public double? ExtractionConfidence { get; set; }
        [JsonPropertyName("medicines")] public List<GeminiExtractedMedicineItem>? Medicines { get; set; }
    }

    private class GeminiExtractedMedicineItem
    {
        public string? OriginalMedicineName { get; set; }
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
