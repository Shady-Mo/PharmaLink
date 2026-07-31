using Application.Services.PatientCatalog;

namespace Infrastructure.Services.PatientCatalog;

public class PatientPharmacyService(
    AppDbContext context,
    ILogger<PatientPharmacyService> logger) : IPatientPharmacyService
{
    public async Task<Result<PaginatedList<NearbyPharmacyDto>>> GetNearbyPharmaciesAsync(
        NearbyPharmaciesRequest request,
        CancellationToken cancellationToken = default)
    {
        // ── validate coordinates ──────────────────────────────────────────────
        if (request.Latitude is < -90 or > 90 || request.Longitude is < -180 or > 180)
            return Result.Failure<PaginatedList<NearbyPharmacyDto>>(
                PatientPharmacyErrors.InvalidCoordinates);

        // clamp radius: 1 km min, 50 km max
        var radiusKm = Math.Clamp(request.RadiusKm, 1, 50);
        var radiusMeters = radiusKm * 1000.0;

        // ── build spatial reference point ─────────────────────────────────────
        var patientLocation = new Point(request.Longitude, request.Latitude) { SRID = 4326 };

        // ── base query ────────────────────────────────────────────────────────
        var query = context.PharmacyBranches
            .AsNoTracking()
            .Include(b => b.WorkingSchedule)
            .Where(b =>
                b.GeoLocation != null &&
                b.Pharmacy.VerificationStatus == Domain.Enums.VerificationStatus.Verified &&
                b.GeoLocation!.Distance(patientLocation) <= radiusMeters);

        // ── optional name search ──────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(b =>
                b.BranchName.Contains(term) ||
                b.Pharmacy.LegalName.Contains(term));
        }

        // ── project, order by distance, paginate ──────────────────────────────
        var now = DateTime.Now;

        var projected = query
            .OrderBy(b => b.GeoLocation!.Distance(patientLocation))
            .Select(b => new NearbyPharmacyDto
            {
                BranchId        = b.BranchId,
                BranchName      = b.BranchName,
                PharmacyName    = b.Pharmacy.LegalName,
                LogoUrl         = b.Pharmacy.LogoUrl,
                AddressLine     = b.AddressLine,
                City            = b.City,
                Governorate     = b.Governorate,
                PhoneNumber     = b.PhoneNumber,
                DistanceKm      = Math.Round(b.GeoLocation!.Distance(patientLocation) / 1000.0, 2),
                WorkingHours    = b.WorkingHours,
                SupportsDelivery = b.SupportsDelivery,
                SupportsPickup  = b.SupportsPickup,
                Latitude        = b.GeoLocation != null ? b.GeoLocation.Y : (double?)null,
                Longitude       = b.GeoLocation != null ? b.GeoLocation.X : (double?)null,
                ServiceRadiusKm = b.ServiceRadiusKm,
            });

        var paged = await projected.ToPaginatedListAsync(
            request.PageNumber, request.PageSize, cancellationToken);

        // ── compute IsOpen + build weekly schedule (post-materialisation) ────
        var branchIds = paged.Items.Select(b => b.BranchId).ToList();
        var scheduleMap = await context.PharmacyBranchSchedules
            .AsNoTracking()
            .Where(s => branchIds.Contains(s.BranchId))
            .ToListAsync(cancellationToken);

        var scheduleByBranch = scheduleMap.GroupBy(s => s.BranchId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var todayDay = now.DayOfWeek;
        var currentMinutes = now.Hour * 60 + now.Minute;
        string[] dayNamesAr = ["الأحد", "الاثنين", "الثلاثاء", "الأربعاء", "الخميس", "الجمعة", "السبت"];

        foreach (var branch in paged.Items)
        {
            scheduleByBranch.TryGetValue(branch.BranchId, out var days);

            if (days is { Count: > 0 })
            {
                // Build weekly schedule DTO
                var dayMap = days.ToDictionary(d => d.Day);
                branch.WeeklySchedule = Enumerable.Range(0, 7).Select(i =>
                {
                    var d = (DayOfWeek)i;
                    dayMap.TryGetValue(d, out var entry);
                    bool closed  = entry?.IsClosed ?? false;
                    bool isToday = d == todayDay;
                    bool open    = isToday && !closed &&
                                   entry?.OpenTime is not null && entry?.CloseTime is not null &&
                                   IsOpenNow(entry.OpenTime.Value, entry.CloseTime.Value, currentMinutes);
                    return new BranchScheduleDayDto
                    {
                        Day             = d,
                        DayNameAr       = dayNamesAr[i],
                        OpenTime        = entry?.OpenTime?.ToString("HH:mm"),
                        CloseTime       = entry?.CloseTime?.ToString("HH:mm"),
                        IsClosed        = closed,
                        IsToday         = isToday,
                        IsCurrentlyOpen = open,
                    };
                }).ToList();

                // Set branch-level IsOpen from today's schedule entry
                var todayEntry = dayMap.GetValueOrDefault(todayDay);
                branch.IsOpen = todayEntry is not null &&
                                !todayEntry.IsClosed &&
                                todayEntry.OpenTime is not null &&
                                todayEntry.CloseTime is not null &&
                                IsOpenNow(todayEntry.OpenTime.Value, todayEntry.CloseTime.Value, currentMinutes);
            }
            else
            {
                // Fallback: parse WorkingHours string
                branch.IsOpen = ComputeIsOpen(branch.WorkingHours, now);
            }
        }

        // ── apply optional IsOpen filter (post-compute) ───────────────────────
        if (request.IsOpen.HasValue)
        {
            var filtered = paged.Items
                .Where(b => b.IsOpen == request.IsOpen.Value)
                .ToList();

            logger.LogInformation(
                "NearbyPharmacies: {Total} branches within {Radius}km, {Filtered} after IsOpen={Filter} filter.",
                paged.TotalCount, radiusKm, filtered.Count, request.IsOpen.Value);

            return Result.Success(
                new PaginatedList<NearbyPharmacyDto>(
                    filtered, paged.PageNumber, filtered.Count, paged.PageSize));
        }

        logger.LogInformation(
            "NearbyPharmacies: returned {Count}/{Total} branches within {Radius}km.",
            paged.Items.Count, paged.TotalCount, radiusKm);

        return Result.Success(paged);
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static bool IsOpenNow(TimeOnly open, TimeOnly close, int currentMinutes)
    {
        int openMin  = open.Hour  * 60 + open.Minute;
        int closeMin = close.Hour * 60 + close.Minute;

        if (closeMin < openMin) // Overnight
            return currentMinutes >= openMin || currentMinutes < closeMin;

        return currentMinutes >= openMin && currentMinutes < closeMin;
    }


    /// <summary>
    /// Parses a simple "H:MM AM/PM - H:MM AM/PM" or "HH:MM - HH:MM" (24h) string
    /// and returns true if <paramref name="now"/> falls within the range.
    /// Returns false for any unparseable format.
    /// </summary>
    private static bool ComputeIsOpen(string workingHours, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(workingHours))
            return false;

        // Support separator " - ", " – ", " to " (case-insensitive)
        var separators = new[] { " - ", " – ", " to ", "-", "–" };
        string? openPart = null, closePart = null;

        foreach (var sep in separators)
        {
            var idx = workingHours.IndexOf(sep, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) continue;
            openPart  = workingHours[..idx].Trim();
            closePart = workingHours[(idx + sep.Length)..].Trim();
            break;
        }

        if (openPart is null || closePart is null) return false;

        if (!TryParseTime(openPart,  out var openTime))  return false;
        if (!TryParseTime(closePart, out var closeTime)) return false;

        var currentMinutes = now.Hour * 60 + now.Minute;

        // Overnight range (e.g. 10 PM – 2 AM)
        if (closeTime < openTime)
            return currentMinutes >= openTime || currentMinutes < closeTime;

        return currentMinutes >= openTime && currentMinutes < closeTime;
    }

    /// <summary>Parses "9:00 AM", "21:30", "9 AM" → total minutes since midnight.</summary>
    private static bool TryParseTime(string raw, out int totalMinutes)
    {
        totalMinutes = 0;
        raw = raw.Trim();

        // Try 12-hour with AM/PM
        if (DateTime.TryParseExact(raw,
                new[] { "h:mm tt", "hh:mm tt", "h tt", "hh tt" },
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out var dt12))
        {
            totalMinutes = dt12.Hour * 60 + dt12.Minute;
            return true;
        }

        // Try 24-hour
        if (DateTime.TryParseExact(raw,
                new[] { "H:mm", "HH:mm" },
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out var dt24))
        {
            totalMinutes = dt24.Hour * 60 + dt24.Minute;
            return true;
        }

        return false;
    }
}
