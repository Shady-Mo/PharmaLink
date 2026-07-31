using Application.DTOs.PharmacyBranch.Request;
using Application.DTOs.PharmacyBranch.Response;
using Application.Services.Pharmacy;

namespace Infrastructure.Services;

public class PharmacyBranchScheduleService(
    AppDbContext context,
    ILogger<PharmacyBranchScheduleService> logger) : IPharmacyBranchScheduleService
{
    private static readonly string[] DayNamesAr =
        ["الأحد", "الاثنين", "الثلاثاء", "الأربعاء", "الخميس", "الجمعة", "السبت"];

    // ──────────────────────────────────────────────────────────────────────────
    public async Task<Result<BranchScheduleResponseDto>> GetScheduleAsync(
        Guid pharmacyId,
        Guid branchId,
        CancellationToken cancellationToken = default)
    {
        var branch = await context.PharmacyBranches
            .AsNoTracking()
            .Include(b => b.WorkingSchedule)
            .FirstOrDefaultAsync(
                b => b.BranchId == branchId && b.PharmacyId == pharmacyId,
                cancellationToken);

        if (branch is null)
            return Result.Failure<BranchScheduleResponseDto>(PharmacyBranchErrors.BranchNotFound);

        return Result.Success(BuildResponse(branch.BranchId, branch.WorkingSchedule));
    }

    // ──────────────────────────────────────────────────────────────────────────
    public async Task<Result<BranchScheduleResponseDto>> UpsertScheduleAsync(
        Guid pharmacyId,
        Guid branchId,
        UpdateBranchScheduleRequest request,
        CancellationToken cancellationToken = default)
    {
        // Validate request contains 7 distinct days
        if (request.Schedule is null || request.Schedule.Count != 7 ||
            request.Schedule.Select(s => s.Day).Distinct().Count() != 7)
        {
            return Result.Failure<BranchScheduleResponseDto>(
                PharmacyBranchScheduleErrors.InvalidSchedule);
        }

        var branch = await context.PharmacyBranches
            .Include(b => b.WorkingSchedule)
            .FirstOrDefaultAsync(
                b => b.BranchId == branchId && b.PharmacyId == pharmacyId,
                cancellationToken);

        if (branch is null)
            return Result.Failure<BranchScheduleResponseDto>(PharmacyBranchErrors.BranchNotFound);

        // Remove existing rows and replace
        context.PharmacyBranchSchedules.RemoveRange(branch.WorkingSchedule);

        foreach (var dayReq in request.Schedule)
        {
            TimeOnly? open  = null;
            TimeOnly? close = null;

            if (!dayReq.IsClosed)
            {
                if (!TryParseTime(dayReq.OpenTime, out open))
                    return Result.Failure<BranchScheduleResponseDto>(
                        PharmacyBranchScheduleErrors.InvalidTimeFormat(dayReq.Day, "الفتح"));

                if (!TryParseTime(dayReq.CloseTime, out close))
                    return Result.Failure<BranchScheduleResponseDto>(
                        PharmacyBranchScheduleErrors.InvalidTimeFormat(dayReq.Day, "الإغلاق"));
            }

            context.PharmacyBranchSchedules.Add(new PharmacyBranchSchedule
            {
                Id       = Guid.NewGuid(),
                BranchId = branchId,
                Day      = dayReq.Day,
                OpenTime = open,
                CloseTime = close,
                IsClosed = dayReq.IsClosed,
            });
        }

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Branch {BranchId} schedule updated by pharmacy {PharmacyId}.",
            branchId, pharmacyId);

        // Re-load for response
        var freshSchedule = await context.PharmacyBranchSchedules
            .AsNoTracking()
            .Where(s => s.BranchId == branchId)
            .ToListAsync(cancellationToken);

        return Result.Success(BuildResponse(branchId, freshSchedule));
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static BranchScheduleResponseDto BuildResponse(
        Guid branchId,
        IEnumerable<PharmacyBranchSchedule> schedules)
    {
        var now = DateTime.Now;
        var todayDay = now.DayOfWeek;
        var currentMinutes = now.Hour * 60 + now.Minute;

        var scheduleMap = schedules.ToDictionary(s => s.Day);

        var days = Enumerable.Range(0, 7).Select(i =>
        {
            var day = (DayOfWeek)i;
            scheduleMap.TryGetValue(day, out var entry);

            var openStr  = entry?.OpenTime?.ToString("HH:mm");
            var closeStr = entry?.CloseTime?.ToString("HH:mm");
            bool isClosed = entry?.IsClosed ?? false;
            bool isToday  = day == todayDay;
            bool isCurrentlyOpen = isToday && !isClosed &&
                                   entry?.OpenTime is not null &&
                                   entry?.CloseTime is not null &&
                                   ComputeIsOpen(entry.OpenTime.Value, entry.CloseTime.Value, currentMinutes);

            return new BranchScheduleDayDto
            {
                Day            = day,
                DayNameAr      = DayNamesAr[i],
                OpenTime       = openStr,
                CloseTime      = closeStr,
                IsClosed       = isClosed,
                IsToday        = isToday,
                IsCurrentlyOpen = isCurrentlyOpen,
            };
        }).ToList();

        return new BranchScheduleResponseDto { BranchId = branchId, Days = days };
    }

    private static bool ComputeIsOpen(TimeOnly open, TimeOnly close, int currentMinutes)
    {
        int openMin  = open.Hour  * 60 + open.Minute;
        int closeMin = close.Hour * 60 + close.Minute;

        // Overnight range (e.g. 22:00 – 02:00)
        if (closeMin < openMin)
            return currentMinutes >= openMin || currentMinutes < closeMin;

        return currentMinutes >= openMin && currentMinutes < closeMin;
    }

    private static bool TryParseTime(string? raw, out TimeOnly? result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(raw)) return false;

        if (TimeOnly.TryParseExact(raw.Trim(), ["HH:mm", "H:mm"], out var t))
        {
            result = t;
            return true;
        }
        return false;
    }
}
