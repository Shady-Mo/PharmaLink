using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;

namespace Infrastructure.Services.PrescriptionAudit;

public class PrescriptionAuditBackgroundService(
    IPrescriptionAuditJobQueue queue,
    IServiceScopeFactory scopeFactory,
    IWebHostEnvironment environment,
    ILogger<PrescriptionAuditBackgroundService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Prescription audit background worker started.");

        await RecoverProcessingReviewsAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            PrescriptionAuditJob job;

            try
            {
                job = await queue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await ProcessJobAsync(job, stoppingToken);
        }
    }

    private async Task RecoverProcessingReviewsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var pendingReviews = await context.PrescriptionReviews
            .AsNoTracking()
            .Where(r => r.ProcessingStatus == PrescriptionProcessingStatus.Processing)
            .Select(r => new
            {
                r.PrescriptionReviewId,
                r.PatientUserId,
                r.PrescriptionImagePath,
                r.OriginalFileName
            })
            .ToListAsync(cancellationToken);

        foreach (var review in pendingReviews)
        {
            var absolutePath = Path.Combine(
                environment.ContentRootPath,
                "wwwroot",
                review.PrescriptionImagePath);

            await queue.EnqueueAsync(
                new PrescriptionAuditJob(
                    review.PrescriptionReviewId,
                    review.PatientUserId,
                    absolutePath,
                    review.PrescriptionImagePath,
                    review.OriginalFileName),
                cancellationToken);
        }

        if (pendingReviews.Count > 0)
        {
            logger.LogInformation("Recovered {Count} pending prescription audit job(s).", pendingReviews.Count);
        }
    }

    private async Task ProcessJobAsync(
        PrescriptionAuditJob job,
        CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var agent = scope.ServiceProvider.GetRequiredService<IPrescriptionAuditAgent>();

            await agent.ProcessExistingReviewAsync(
                job.PrescriptionReviewId,
                job.AbsoluteFilePath,
                job.OriginalFileName,
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Prescription audit background job failed. ReviewId={ReviewId}",
                job.PrescriptionReviewId);

            await MarkReviewAsFailedAsync(job.PrescriptionReviewId, ex.Message, CancellationToken.None);
        }
    }

    private async Task MarkReviewAsFailedAsync(
        Guid reviewId,
        string reason,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var review = await context.PrescriptionReviews
            .FirstOrDefaultAsync(r => r.PrescriptionReviewId == reviewId, cancellationToken);

        if (review is null)
        {
            return;
        }

        review.ProcessingStatus = PrescriptionProcessingStatus.Failed;
        review.ReviewNotes = reason;
        review.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
    }
}
