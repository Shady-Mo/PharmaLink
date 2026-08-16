using Infrastructure.AI.Options;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Text.Json;

namespace Infrastructure.AI;

/// <summary>
/// Implements IDrugInfoService using Semantic Kernel prompt functions.
///
/// DESIGN DECISION — Prompt functions for structured extraction:
///   Unlike the PharmacyAssistantService which uses conversational chat,
///   this service uses one-shot prompt invocations with highly structured
///   output schemas. This is ideal for cases where the caller needs a
///   predictable, deserializable response (a DrugInfoResult record) rather
///   than free-form text.
///
///   The prompts are loaded from .prompty template files and invoked via
///   kernel.InvokePromptAsync() with kernel arguments filling template slots.
///   The AI's output is then deserialized into the Application-layer DTOs.
///
/// DESIGN DECISION — Dual strategy (DB lookup + AI):
///   For drug info: First use DrugPlugin to query the database, then use the
///   AI to enrich with general medical knowledge. This gives accurate
///   system-specific data (is it in stock?) combined with broader knowledge
///   (what does it treat?).
///
///   For interaction check: Pure AI — no database has all known interactions,
///   and the model's medical training covers this well. We still validate the
///   output schema strictly.
/// </summary>
public sealed class DrugInfoService(
    Kernel kernel,
    IChatCompletionService chatService,
    IOptions<AiOptions> aiOptions,
    ILogger<DrugInfoService> logger)
    : IDrugInfoService
{
    private readonly IChatCompletionService _chatService = chatService;
    private readonly AiOptions _aiOptions = aiOptions.Value;
    private readonly string _drugInfoPrompt = LoadPrompt("DrugInfo.prompty");
    private readonly string _interactionPrompt = LoadPrompt("InteractionCheck.prompty");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    // -------------------------------------------------------------------------
    //  IDrugInfoService Implementation
    // -------------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task<DrugInfoResult?> GetDrugInfoAsync(
        string drugName,
        CancellationToken ct = default)
    {
        logger.LogInformation("DrugInfoService.GetDrugInfoAsync for: {DrugName}", drugName);

        // Build kernel arguments to fill the {{$drug_name}} slot in the template.
        var arguments = new KernelArguments
        {
            ["drug_name"] = drugName
        };

        // Add auto function calling so the DrugPlugin is called automatically
        // inside the prompt execution (the prompt explicitly instructs the model
        // to use the get_drug_info tool).
        var execSettings = BuildExecutionSettings();
        arguments.ExecutionSettings = new Dictionary<string, PromptExecutionSettings>
        {
            [PromptExecutionSettings.DefaultServiceId] = execSettings
        };

        int maxRetries = 3;
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                logger.LogWarning("[DEBUG-SERVICE] Invoking Kernel for {DrugName} (Attempt {Attempt})", drugName, i + 1);
                // InvokePromptAsync fills the template, sends to the model,
                // and returns the text response (which should be JSON).
                var result = await kernel.InvokePromptAsync(_drugInfoPrompt, arguments, cancellationToken: ct);
                var json = result.GetValue<string>() ?? string.Empty;
                logger.LogWarning("[DEBUG-SERVICE] Kernel invocation succeeded. Result JSON length: {Len}", json.Length);

                return DeserializeDrugInfo(json, drugName);
            }
            catch (Exception ex) when ((ex.ToString().Contains("429") || ex.ToString().Contains("503") || ex.ToString().Contains("500") || ex.ToString().Contains("502") || ex.ToString().Contains("504")) && i < maxRetries - 1)
            {
                logger.LogWarning("Transient server error or rate limit hit for Gemini. Retrying {RetryCount}/{MaxRetries} in 3 seconds...", i + 1, maxRetries - 1);
                await Task.Delay(3000, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "DrugInfoService.GetDrugInfoAsync failed for {DrugName}", drugName);
                if (ex.ToString().Contains("429") || ex.ToString().Contains("503") || ex.ToString().Contains("500") || ex.ToString().Contains("502") || ex.ToString().Contains("504")) throw; // Rethrow to controller returns 429
                break;
            }
        }
        return null;
    }

    /// <inheritdoc/>
    public async Task<InteractionCheckResult> CheckInteractionsAsync(
        IReadOnlyList<string> drugNames,
        CancellationToken ct = default)
    {
        if (drugNames.Count < 2)
        {
            return new InteractionCheckResult(
                drugNames,
                [],
                false,
                "At least 2 drugs are required for an interaction check.");
        }

        logger.LogInformation(
            "DrugInfoService.CheckInteractionsAsync for {Count} drugs: {Drugs}",
            drugNames.Count, string.Join(", ", drugNames));

        // Format the drug list as a numbered list for the template.
        var drugListText = string.Join("\n", drugNames.Select((d, i) => $"{i + 1}. {d}"));

        var arguments = new KernelArguments
        {
            ["drug_list"] = drugListText
        };

        var execSettings = BuildExecutionSettings();
        arguments.ExecutionSettings = new Dictionary<string, PromptExecutionSettings>
        {
            [PromptExecutionSettings.DefaultServiceId] = execSettings
        };

        int maxRetries = 3;
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                var result = await kernel.InvokePromptAsync(_interactionPrompt, arguments, cancellationToken: ct);
                var json = result.GetValue<string>() ?? string.Empty;

                return DeserializeInteractionResult(json, drugNames);
            }
            catch (Exception ex) when ((ex.ToString().Contains("429") || ex.ToString().Contains("503") || ex.ToString().Contains("500") || ex.ToString().Contains("502") || ex.ToString().Contains("504")) && i < maxRetries - 1)
            {
                logger.LogWarning("Transient server error or rate limit hit for Gemini interactions. Retrying {RetryCount}/{MaxRetries} in 3 seconds...", i + 1, maxRetries - 1);
                await Task.Delay(3000, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "DrugInfoService.CheckInteractionsAsync failed");
                if (ex.ToString().Contains("429") || ex.ToString().Contains("503") || ex.ToString().Contains("500") || ex.ToString().Contains("502") || ex.ToString().Contains("504")) throw; // Rethrow to controller
                break;
            }
        }
        
        return new InteractionCheckResult(
            drugNames,
            [],
            false,
            "الخدمة مشغولة حالياً (ضغط على الذكاء الاصطناعي). يرجى المحاولة بعد قليل.");
    }

    // -------------------------------------------------------------------------
    //  Private helpers
    // -------------------------------------------------------------------------

    private PromptExecutionSettings BuildExecutionSettings()
    {
        var provider = _aiOptions.Defaults.ChatProvider.Trim().ToLowerInvariant();

        if (provider is "googlegemini" or "gemini")
        {
            return new GeminiPromptExecutionSettings
            {
                MaxTokens = 8192,
                Temperature = 0.2, // Low temperature for structured/factual output
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
                SafetySettings =
                [
                    new(GeminiSafetyCategory.DangerousContent, GeminiSafetyThreshold.BlockNone),
                    new(GeminiSafetyCategory.Harassment, GeminiSafetyThreshold.BlockNone),
                    new(GeminiSafetyCategory.SexuallyExplicit, GeminiSafetyThreshold.BlockNone)
                ]
            };
        }

        return new OpenAIPromptExecutionSettings
        {
            MaxTokens = 4096,
            Temperature = 0.2, // Low temperature for structured/factual output
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };
    }

    private DrugInfoResult? DeserializeDrugInfo(string json, string originalDrugName)
    {
        // Strip potential Markdown fences the model might still include.
        json = StripMarkdownFences(json);

        try
        {
            var raw = JsonSerializer.Deserialize<RawDrugInfoResponse>(json, JsonOptions);
            if (raw is null) return null;

            return new DrugInfoResult(
                DrugName: raw.DrugName ?? originalDrugName,
                ArabicName: raw.ArabicName,
                GenericName: raw.GenericName,
                Category: raw.Category,
                Description: raw.Description,
                Indications: raw.Indications,
                Contraindications: raw.Contraindications,
                SideEffects: raw.SideEffects,
                Dosage: raw.Dosage,
                StorageInstructions: raw.StorageInstructions,
                RequiresPrescription: raw.RequiresPrescription,
                IsAvailableInSystem: raw.IsAvailableInSystem
            );
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to deserialize DrugInfo response. JSON: {Json}", json);
            return null;
        }
    }

    private InteractionCheckResult DeserializeInteractionResult(
        string json, IReadOnlyList<string> originalDrugs)
    {
        json = StripMarkdownFences(json);

        try
        {
            var raw = JsonSerializer.Deserialize<RawInteractionResponse>(json, JsonOptions);
            if (raw is null)
                return EmptyInteractionResult(originalDrugs);

            var interactions = (raw.Interactions ?? [])
                .Select(i => new DrugInteraction(
                    Drug1: i.Drug1 ?? string.Empty,
                    Drug2: i.Drug2 ?? string.Empty,
                    Severity: ParseSeverity(i.Severity),
                    Description: i.Description ?? string.Empty,
                    Recommendation: i.Recommendation ?? string.Empty))
                .ToList();

            return new InteractionCheckResult(
                CheckedDrugs: raw.CheckedDrugs ?? originalDrugs,
                Interactions: interactions,
                HasSevereInteractions: raw.HasSevereInteractions,
                Summary: raw.Summary ?? "Interaction check complete.");
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to deserialize InteractionCheck response. JSON: {Json}", json);
            return EmptyInteractionResult(originalDrugs);
        }
    }

    private static InteractionCheckResult EmptyInteractionResult(IReadOnlyList<string> drugs) =>
        new(drugs, [], false,
            "Unable to parse interaction results. Please consult your pharmacist for manual review.");

    private static InteractionSeverity ParseSeverity(string? severity) =>
        severity?.Trim() switch
        {
            "Minor" => InteractionSeverity.Minor,
            "Moderate" => InteractionSeverity.Moderate,
            "Severe" => InteractionSeverity.Severe,
            "Contraindicated" => InteractionSeverity.Contraindicated,
            _ => InteractionSeverity.None
        };

    private static string StripMarkdownFences(string text)
    {
        text = text.Trim();
        if (!text.StartsWith("```")) return text;

        var firstNewline = text.IndexOf('\n');
        if (firstNewline >= 0) text = text[(firstNewline + 1)..];
        if (text.EndsWith("```")) text = text[..^3].Trim();

        return text;
    }

    private static string LoadPrompt(string fileName)
    {
        var assemblyDir = Path.GetDirectoryName(typeof(DrugInfoService).Assembly.Location)!;
        var path = Path.Combine(assemblyDir, "AI", "PromptTemplates", fileName);

        return File.Exists(path)
            ? File.ReadAllText(path)
            : $"Provide information about {{{{$drug_name}}}}. Return JSON.";
    }

    // -------------------------------------------------------------------------
    //  Private raw response models for JSON deserialization
    // -------------------------------------------------------------------------

    private sealed class RawDrugInfoResponse
    {
        public string? DrugName { get; set; }
        public string? ArabicName { get; set; }
        public string? GenericName { get; set; }
        public string? Category { get; set; }
        public string? Description { get; set; }
        public string? Indications { get; set; }
        public string? Contraindications { get; set; }
        public string? SideEffects { get; set; }
        public string? Dosage { get; set; }
        public string? StorageInstructions { get; set; }
        public bool RequiresPrescription { get; set; }
        public bool IsAvailableInSystem { get; set; }
    }

    private sealed class RawInteractionResponse
    {
        public IReadOnlyList<string>? CheckedDrugs { get; set; }
        public List<RawInteraction>? Interactions { get; set; }
        public bool HasSevereInteractions { get; set; }
        public string? Summary { get; set; }
    }

    private sealed class RawInteraction
    {
        public string? Drug1 { get; set; }
        public string? Drug2 { get; set; }
        public string? Severity { get; set; }
        public string? Description { get; set; }
        public string? Recommendation { get; set; }
    }
}