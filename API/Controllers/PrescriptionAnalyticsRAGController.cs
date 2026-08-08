using Application.DTOs.AI.RAG;
using Application.Services.AI.RAG;
using Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v1/ai/prescription-analytics")]
[Authorize]
public class PrescriptionAnalyticsRAGController : ControllerBase
{
    private readonly IPrescriptionAnalyticsRagService _ragService;
    private readonly ILogger<PrescriptionAnalyticsRAGController> _logger;

    public PrescriptionAnalyticsRAGController(
        IPrescriptionAnalyticsRagService ragService,
        ILogger<PrescriptionAnalyticsRAGController> logger)
    {
        _ragService = ragService;
        _logger = logger;
    }

    /// <summary>
    /// Ask a natural language RAG analytics question about uploaded prescription trends,
    /// pediatric medication demand, and inventory shortages for a branch or region.
    /// Accessible by Pharmacists, Pharmacy Admins, System Admins, and Review Team.
    /// </summary>
    [HttpPost("query")]
    [Authorize(Roles = $"{AppRoles.Pharmacist},{AppRoles.PharmacyAdmin},{AppRoles.Admin},{AppRoles.PrescriptionReviewTeam}")]
    [ProducesResponseType(typeof(PrescriptionAnalyticsRagResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> QueryAnalytics(
        [FromBody] PrescriptionAnalyticsRagRequestDTO request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
        {
            return BadRequest("السؤال مطلوب للبحث في الـ RAG.");
        }

        try
        {
            var result = await _ragService.QueryAnalyticsAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt for prescription analytics query: '{Question}'", request.Question);
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing prescription analytics RAG query: '{Question}'", request.Question);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "حدث خطأ أثناء معالجة استعلام الـ RAG للروشتات.",
                details = ex.Message
            });
        }
    }

    /// <summary>
    /// Re-indexes all existing prescription review records into the RAG vector store.
    /// Restricted to System Administrators.
    /// </summary>
    [HttpPost("reindex")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ReindexPrescriptions(CancellationToken cancellationToken)
    {
        var indexedCount = await _ragService.ReindexPrescriptionsAsync(cancellationToken);
        return Ok(new
        {
            message = "تمت عملية إعادة تكشيف الروشتات في محرك الـ RAG بنجاح.",
            totalIndexed = indexedCount
        });
    }
}
