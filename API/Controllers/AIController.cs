using API.DTOs.AI;
using Application.DTOs.AI;
using Application.Services.AI;
using Application.Services.AI.Models;
using Application.Services.PrescriptionAudit;
using Microsoft.SemanticKernel;

namespace API.Controllers;

[Route("api/v1/ai")]
public class AIController(
    IPrescriptionExtractionService prescriptionExtractionService,
    IMedicineImageExtractionService medicineImageExtractionService,
    IAgentProfileProvider agentProfileProvider,
    IDrugCatalogPlugin drugCatalogPlugin,
    IConfiguration configuration)
    : ControllerBase
{
    [HttpGet("agents")]
    [ProducesResponseType(typeof(IReadOnlyList<AgentProfile>), StatusCodes.Status200OK)]
    public IActionResult GetAgents()
    {
        return Ok(agentProfileProvider.GetAll());
    }

    [HttpGet("agents/{codeName}")]
    [ProducesResponseType(typeof(AgentProfile), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetAgent(string codeName)
    {
        var profile = agentProfileProvider.GetByCodeName(codeName);

        if (profile is null)
        {
            return NotFound("الـ AI Agent المطلوب غير موجود.");
        }

        return Ok(profile);
    }

    [HttpPost("extract-prescription")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ExtractPrescriptionResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExtractPrescription(
        [FromForm] ExtractPrescriptionRequest request,
        CancellationToken cancellationToken)
    {
        if (request.File is null || request.File.Length == 0)
        {
            return BadRequest("Prescription file is required.");
        }

        var file = await ToAIFileContentAsync(request.File, cancellationToken);
        AIExtractionResult result;

        try
        {
            result = await prescriptionExtractionService.ExtractAsync(file, cancellationToken);
        }
        catch (Exception ex) when (IsAIProviderException(ex))
        {
            return AIProviderUnavailable(ex, "Vision:ExtractPrescription");
        }

        return Ok(new ExtractPrescriptionResponseDTO
        {
            IsValidPrescription = result.IsValidPrescription,
            ValidationMessage = result.ValidationMessage,
            ExtractedText = result.ExtractedText,
            AiSummary = result.AISummary,
            ExtractionConfidence = result.ExtractionConfidence,
            Medicines = result.Medicines.Select(m => new ExtractedPrescriptionMedicineDTO
            {
                MedicineName = m.MedicineName,
                GenericName = m.GenericName,
                Strength = m.Strength,
                DosageForm = m.DosageForm,
                Dose = m.Dose,
                Frequency = m.Frequency,
                Duration = m.Duration,
                Quantity = m.Quantity,
                Route = m.Route,
                Confidence = m.Confidence
            }).ToList()
        });
    }

    [HttpPost("extract-medicine-image")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(MedicineImageExtractionResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExtractMedicineImage(
        [FromForm] ExtractMedicineImageRequest request,
        CancellationToken cancellationToken)
    {
        if (request.File is null || request.File.Length == 0)
        {
            return BadRequest("Medicine image file is required.");
        }

        var file = await ToAIFileContentAsync(request.File, cancellationToken);
        MedicineImageExtractionResponseDTO result;

        try
        {
            result = await medicineImageExtractionService.ExtractAsync(file, cancellationToken);
        }
        catch (Exception ex) when (IsAIProviderException(ex))
        {
            return AIProviderUnavailable(ex, "Vision:ExtractMedicineImage");
        }

        return Ok(result);
    }

    [HttpPost("scan-medicine-image")]
    [Authorize(Roles = AppRoles.Patient)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(MedicineImageScanResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ScanMedicineImage(
        [FromForm] ExtractMedicineImageRequest request,
        CancellationToken cancellationToken)
    {
        if (request.File is null || request.File.Length == 0)
        {
            return BadRequest("Medicine image file is required.");
        }

        var file = await ToAIFileContentAsync(request.File, cancellationToken);
        MedicineImageExtractionResponseDTO extraction;

        try
        {
            extraction = await medicineImageExtractionService.ExtractAsync(file, cancellationToken);
        }
        catch (Exception ex) when (IsAIProviderException(ex))
        {
            return AIProviderUnavailable(ex, "Vision:ExtractMedicineImage");
        }

        var match = await drugCatalogPlugin.FindBestMatchAsync(
            new ExtractedMedicineItem
            {
                MedicineName = extraction.MedicineName,
                Strength = extraction.Strength,
                DosageForm = extraction.DosageForm,
                Confidence = extraction.Confidence
            },
            cancellationToken);

        var canBeAddedToCart = match.Status is PrescriptionMedicineMatchStatus.ExactMatch
            or PrescriptionMedicineMatchStatus.FuzzyMatch
            or PrescriptionMedicineMatchStatus.AlternativeSuggested;

        var cartDrugId = match.Status == PrescriptionMedicineMatchStatus.AlternativeSuggested
            ? match.SuggestedAlternativeDrugId
            : match.DrugId;

        return Ok(new MedicineImageScanResponseDTO
        {
            Extraction = extraction,
            MatchedDrugId = match.DrugId,
            SuggestedAlternativeDrugId = match.SuggestedAlternativeDrugId,
            CartDrugId = canBeAddedToCart ? cartDrugId : null,
            MatchStatus = match.Status.ToString(),
            MatchScore = match.Score,
            MatchReason = match.Reason,
            CanBeAddedToCart = canBeAddedToCart && cartDrugId.HasValue
        });
    }

    private static async Task<AIFileContent> ToAIFileContentAsync(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, cancellationToken);

        return new AIFileContent
        {
            FileName = file.FileName,
            ContentType = string.IsNullOrWhiteSpace(file.ContentType)
                ? ResolveContentType(file.FileName)
                : file.ContentType,
            Content = memory.ToArray()
        };
    }

    private static string ResolveContentType(string fileName)
    {
        return Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".heic" => "image/heic",
            ".pdf" => "application/pdf",
            _ => "image/jpeg"
        };
    }

    private ObjectResult AIProviderUnavailable(Exception ex, string routeKey)
    {
        var logger = HttpContext.RequestServices.GetRequiredService<ILogger<AIController>>();
        logger.LogError(ex, "AI Provider for route {RouteKey} is unavailable. Error: {Message}", routeKey, ex.Message);

        return StatusCode(StatusCodes.Status503ServiceUnavailable, new
        {
            message = "مزود الذكاء الاصطناعي مش متاح حاليًا. جرّب تاني بعد شوية أو غيّر الـ AI provider من الإعدادات.",
            configuredProvider = configuration[$"AI:TaskRouting:{routeKey}:Provider"]
                ?? configuration["AI:Defaults:VisionProvider"],
            configuredModel = configuration[$"AI:TaskRouting:{routeKey}:ModelId"],
            providerError = ex.Message
        });
    }

    private static bool IsAIProviderException(Exception ex)
    {
        return ex is HttpOperationException
            || ex is HttpRequestException
            || ex is TaskCanceledException
            || ex is InvalidOperationException
            && (ex.Message.Contains("AI", StringComparison.OrdinalIgnoreCase)
                || ex.Message.Contains("provider", StringComparison.OrdinalIgnoreCase)
                || ex.Message.Contains("Gemini", StringComparison.OrdinalIgnoreCase)
                || ex.Message.Contains("Groq", StringComparison.OrdinalIgnoreCase)
                || ex.Message.Contains("ITI", StringComparison.OrdinalIgnoreCase));
    }
}
