using Application.DTOs.RecurringPrescription;

namespace Infrastructure.Services;

public class RecurringPrescriptionService(
    AppDbContext context,
    IEmailService emailService,
    IOrderSplittingService orderSplittingService,
    ILogger<RecurringPrescriptionService> logger) : IRecurringPrescriptionService
{
    public async Task<Result<RecurringResponseDto>> CreateAsync(Guid patientId, CreateRecurringRequest request)
    {
        var recurring = new RecurringPrescription
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            Name = request.Name,
            Notes = request.Notes,
            PrescriptionId = request.PrescriptionId,
            IntervalDays = request.IntervalDays,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            NextRunDate = request.StartDate,
            FulfillmentMode = request.FulfillmentMode,
            PreferredBranchId = request.PreferredBranchId,
            DeliveryAddressId = request.DeliveryAddressId,
            RequireConfirmation = request.RequireConfirmation,
            Status = RecurringStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        context.RecurringPrescriptions.Add(recurring);
        await context.SaveChangesAsync();
        return Result.Success(MapToDto(recurring));
    }

    public async Task<Result<RecurringResponseDto>> UpdateAsync(Guid id, Guid patientId, CreateRecurringRequest request)
    {
        var recurring = await context.RecurringPrescriptions
            .FirstOrDefaultAsync(r => r.Id == id && r.PatientId == patientId);
        if (recurring is null)
            return Result.Failure<RecurringResponseDto>(new Error("RecurringPrescription.NotFound",
                $"RecurringPrescription with id {id} not found.", 404));

        recurring.Name = request.Name;
        recurring.Notes = request.Notes;
        recurring.IntervalDays = request.IntervalDays;
        recurring.EndDate = request.EndDate;
        recurring.FulfillmentMode = request.FulfillmentMode;
        recurring.PreferredBranchId = request.PreferredBranchId;
        recurring.DeliveryAddressId = request.DeliveryAddressId;
        recurring.RequireConfirmation = request.RequireConfirmation;

        await context.SaveChangesAsync();
        return Result.Success(MapToDto(recurring));
    }

    public async Task<Result> PauseAsync(Guid id, Guid patientId)
    {
        var recurring = await context.RecurringPrescriptions
            .FirstOrDefaultAsync(r => r.Id == id && r.PatientId == patientId);
        if (recurring is null)
            return Result.Failure(new Error("RecurringPrescription.NotFound",
                $"RecurringPrescription with id {id} not found.", 404));

        recurring.Status = RecurringStatus.Paused;
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> ResumeAsync(Guid id, Guid patientId)
    {
        var recurring = await context.RecurringPrescriptions
            .FirstOrDefaultAsync(r => r.Id == id && r.PatientId == patientId);
        if (recurring is null)
            return Result.Failure(new Error("RecurringPrescription.NotFound",
                $"RecurringPrescription with id {id} not found.", 404));

        recurring.Status = RecurringStatus.Active;
        // Recalculate next run date if in past
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (recurring.NextRunDate < today)
        {
            recurring.NextRunDate = today;
        }

        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid id, Guid patientId)
    {
        var recurring = await context.RecurringPrescriptions
            .FirstOrDefaultAsync(r => r.Id == id && r.PatientId == patientId);
        if (recurring is null)
            return Result.Failure(new Error("RecurringPrescription.NotFound",
                $"RecurringPrescription with id {id} not found.", 404));

        context.RecurringPrescriptions.Remove(recurring);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<List<RecurringResponseDto>>> GetPatientRecurringAsync(Guid patientId)
    {
        var recurring = await context.RecurringPrescriptions
            .Include(r => r.PreferredBranch)
            .Include(r => r.Runs.OrderByDescending(run => run.ScheduledAt).Take(5))
            .Where(r => r.PatientId == patientId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return Result.Success(recurring.Select(MapToDto).ToList());
    }

    public async Task<Result> ConfirmRunAsync(Guid runId, string token)
    {
        var run = await context.RecurringPrescriptionRuns
            .Include(r => r.RecurringPrescription)
            .FirstOrDefaultAsync(r => r.Id == runId && r.ConfirmationToken == token);

        if (run is null) return Result.Failure(new Error("Run.NotFound", $"Run with id {runId} not found.", 404));
        
        if (run.Status != RecurringRunStatus.PendingConfirmation)
            return Result.Failure(new Error("Run.Validation", "Run is not pending confirmation", 400));

        run.Status = RecurringRunStatus.Confirmed;
        run.ProcessedAt = DateTime.UtcNow;

        var orderId = await ProcessRunOrder(run.RecurringPrescription);
        run.OrderId = orderId;

        // Advance next run date
        run.RecurringPrescription.NextRunDate =
            DateOnly.FromDateTime(DateTime.UtcNow).AddDays(run.RecurringPrescription.IntervalDays);

        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> SkipRunAsync(Guid runId, Guid patientId)
    {
        var run = await context.RecurringPrescriptionRuns
            .Include(r => r.RecurringPrescription)
            .FirstOrDefaultAsync(r => r.Id == runId && r.RecurringPrescription.PatientId == patientId);

        if (run is null) return Result.Failure(new Error("Run.NotFound", $"Run with id {runId} not found.", 404));

        run.Status = RecurringRunStatus.Skipped;
     
        run.ProcessedAt = DateTime.UtcNow;
     
        run.RecurringPrescription.NextRunDate =
            DateOnly.FromDateTime(DateTime.UtcNow).AddDays(run.RecurringPrescription.IntervalDays);

        await context.SaveChangesAsync();
       
        return Result.Success();
    }

    public async Task ProcessDueRecurringAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
      
        var recurrings = await context.RecurringPrescriptions
            .Include(r => r.Patient)
            .Where(r => r.Status == RecurringStatus.Active && r.NextRunDate <= today &&
                        (r.EndDate == null || r.EndDate >= today))
            .ToListAsync();

        foreach (var recurring in recurrings)
        {
            var run = new RecurringPrescriptionRun
            {
                Id = Guid.NewGuid(),
                RecurringPrescriptionId = recurring.Id,
                ScheduledAt = DateTime.UtcNow,
                Status = recurring.RequireConfirmation
                    ? RecurringRunStatus.PendingConfirmation
                    : RecurringRunStatus.Confirmed
            };

            if (recurring.RequireConfirmation)
            {
                run.ConfirmationToken = Guid.NewGuid().ToString("N");
                run.ConfirmationDeadline = DateTime.UtcNow.AddHours(24);

                if (!string.IsNullOrEmpty(recurring.Patient.Email))
                {
                    var confirmLink =
                        $"https://pharmalink.com/api/recurring-prescriptions/runs/{run.Id}/confirm?token={run.ConfirmationToken}";
                    var body =
                        $"<h2>تأكيد طلب الروشتة الدورية: {recurring.Name}</h2><p>الرجاء الضغط على الرابط لتأكيد الطلب:</p><a href='{confirmLink}'>تأكيد الطلب</a>";
                    await emailService.SendEmailAsync(recurring.Patient.Email, $"تأكيد طلب: {recurring.Name}", body);
                }
            }
            else
            {
                var orderId = await ProcessRunOrder(recurring);
                run.OrderId = orderId;
                run.ProcessedAt = DateTime.UtcNow;
                recurring.NextRunDate = today.AddDays(recurring.IntervalDays);
            }

            context.RecurringPrescriptionRuns.Add(run);
        }

        await context.SaveChangesAsync();
    }

    public async Task AutoConfirmExpiredRunsAsync()
    {
        var now = DateTime.UtcNow;
       
        var expiredRuns = await context.RecurringPrescriptionRuns
            .Include(r => r.RecurringPrescription)
            .Where(r => r.Status == RecurringRunStatus.PendingConfirmation && r.ConfirmationDeadline <= now)
            .ToListAsync();

        foreach (var run in expiredRuns)
        {
            run.Status = RecurringRunStatus.Confirmed;
            run.ProcessedAt = now;
            var orderId = await ProcessRunOrder(run.RecurringPrescription);
            run.OrderId = orderId;
            run.RecurringPrescription.NextRunDate =
                DateOnly.FromDateTime(now).AddDays(run.RecurringPrescription.IntervalDays);
        }

        await context.SaveChangesAsync();
    }

    private async Task<Guid?> ProcessRunOrder(RecurringPrescription recurring)
    {
        try
        {
            var order = new Order
            {
                OrderId = Guid.NewGuid(),
                PatientUserId = recurring.PatientId,
                DeliveryAddressId = recurring.DeliveryAddressId ?? Guid.Empty,
                FulfillmentMode = recurring.FulfillmentMode,
                OrderStatus = OrderStatus.PendingPrescriptionReview,
                TotalAmount = 0
            };

            context.Orders.Add(order);

            if (recurring.PrescriptionId.HasValue)
            {
                var originalPrescription =
                    await context.Prescriptions.FirstOrDefaultAsync(p => p.Id == recurring.PrescriptionId.Value);
                if (originalPrescription != null)
                {
                    // Create a clone for the new order
                    var newPrescription = new Prescription
                    {
                        Id = Guid.NewGuid(),
                        PatientId = recurring.PatientId,
                        FileUrl = originalPrescription.FileUrl,
                        FileName = originalPrescription.FileName,
                        OrderId = order.OrderId,
                        Status = PrescriptionStatus.PendingReview
                    };
                    context.Prescriptions.Add(newPrescription);
                }
            }

            if (recurring.PreferredBranchId.HasValue)
            {
                // Directly assign to branch
                var leg = new OrderFulfillmentLeg
                {
                    LegId = Guid.NewGuid(),
                    OrderId = order.OrderId,
                    BranchId = recurring.PreferredBranchId.Value,
                    LegStatus = LegStatus.Assigned,
                    LegType = LegType.Preparation
                };
                context.OrderFulfillmentLegs.Add(leg);
                order.OrderStatus = OrderStatus.Processing;
            }

            await context.SaveChangesAsync();

            if (!recurring.PreferredBranchId.HasValue)
            {
                // Trigger AI Routing
                await orderSplittingService.SplitOrderAsync(order.OrderId);
            }

            return order.OrderId;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating order for recurring prescription {Id}", recurring.Id);
            return null;
        }
    }

    private static RecurringResponseDto MapToDto(RecurringPrescription r)
    {
        return new RecurringResponseDto
        {
            Id = r.Id,
            Name = r.Name,
            Notes = r.Notes,
            IntervalDays = r.IntervalDays,
            StartDate = r.StartDate,
            EndDate = r.EndDate,
            NextRunDate = r.NextRunDate,
            FulfillmentMode = r.FulfillmentMode.ToString(),
            PreferredBranchId = r.PreferredBranchId,
            PreferredBranchName = r.PreferredBranch?.BranchName,
            RequireConfirmation = r.RequireConfirmation,
            Status = r.Status.ToString(),
            CreatedAt = r.CreatedAt,
            RecentRuns = r.Runs?.Select(run => new RecurringRunDto
            {
                Id = run.Id,
                Status = run.Status.ToString(),
                ScheduledAt = run.ScheduledAt,
                ProcessedAt = run.ProcessedAt,
                OrderId = run.OrderId
            }).ToList() ?? []
        };
    }
}