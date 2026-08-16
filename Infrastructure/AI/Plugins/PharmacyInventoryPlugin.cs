using System.ComponentModel;
using System.Text.Json;
using Application.DTOs.OrderRouting;
using Application.DTOs.Routing;
using Infrastructure.Services;
using Microsoft.SemanticKernel;

namespace Infrastructure.AI.Plugins;

public sealed class PharmacyInventoryPlugin(
    IServiceScopeFactory scopeFactory,
    IOsrmRoutingService osrmRoutingService,
    ILogger<PharmacyInventoryPlugin> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private const double MaxDistanceKm = 50.0;

    public FulfillmentMode DefaultFulfillmentMode { get; init; } = FulfillmentMode.Delivery;

    public OsrmMatrixResultDto? LastMatrixResult { get; private set; }
    public Dictionary<Guid, int>? LastBranchCoordIndex { get; private set; }

    [KernelFunction("evaluate_candidate_branches")]
    [Description(
        "Evaluates every nearby pharmacy branch against the patient's cart. For each branch it " +
        "reports how many cart drugs it can fully supply, which drugs it is missing, the live " +
        "stock, and the distance (km) from the patient. Returns a JSON array of branch " +
        "evaluations ordered by coverage (desc) then distance (asc). Use this to decide how to " +
        "route/split the order.")]
    public async Task<string> EvaluateCandidateBranchesAsync(
        [Description("Patient delivery latitude, in decimal degrees.")]
        double patientLatitude,
        [Description("Patient delivery longitude, in decimal degrees.")]
        double patientLongitude,
        [Description(
            "JSON array of cart items to route, each as {\"drugId\":\"<guid>\",\"drugName\":\"<name>\",\"quantity\":<int>}.")]
        string cartItemsJson,
        CancellationToken cancellationToken = default)
    {
        var cartItems = DeserializeCart(cartItemsJson);
        var evaluations = await EvaluateAsync(
            new GeoLocation(patientLatitude, patientLongitude), cartItems, DefaultFulfillmentMode, cancellationToken);

        return JsonSerializer.Serialize(evaluations, JsonOptions);
    }

    public async Task<IReadOnlyList<BranchFulfillmentEvaluation>> EvaluateAsync(
        GeoLocation patientLocation,
        IReadOnlyList<CartItemDto> cartItems,
        FulfillmentMode fulfillmentMode,
        CancellationToken cancellationToken = default)
    {
        if (cartItems.Count == 0)
            return [];

        logger.LogInformation(
            "PharmacyInventoryPlugin.EvaluateAsync — {ItemCount} cart items from ({Lat},{Lng}), mode={Mode}",
            cartItems.Count, patientLocation.Latitude, patientLocation.Longitude, fulfillmentMode);

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var drugIds = cartItems.Select(c => c.DrugId).Distinct().ToList();
        var quantityByDrug = cartItems
            .GroupBy(c => c.DrugId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));
        var cartNameByDrug = cartItems
            .GroupBy(c => c.DrugId)
            .ToDictionary(g => g.Key, g => g.First().DrugName);

        var drugNames = await db.Drugs
            .AsNoTracking()
            .Where(d => drugIds.Contains(d.DrugId))
            .Select(d => new { d.DrugId, d.BrandName, d.ArabicName })
            .ToDictionaryAsync(d => d.DrugId, d => new DrugNames(d.BrandName, d.ArabicName), cancellationToken);

        (string En, string Ar) NamesFor(Guid drugId) =>
            drugNames.TryGetValue(drugId, out var n)
                ? (string.IsNullOrWhiteSpace(n.En) ? cartNameByDrug.GetValueOrDefault(drugId, string.Empty) : n.En, n.Ar)
                : (cartNameByDrug.GetValueOrDefault(drugId, string.Empty), string.Empty);

        var today = DateTime.UtcNow.DayOfWeek;

        var stockRows = await db.PharmacyInventories
            .AsNoTracking()
            .Where(i => drugIds.Contains(i.DrugId) && (i.StockQuantity - i.ReservedQuantity) > 0)
            .Select(i => new StockRow(
                i.Branch.PharmacyId,
                i.Branch.Pharmacy.LegalName,
                i.BranchId,
                i.Branch.BranchName,
                i.Branch.GeoLocation != null ? i.Branch.GeoLocation.Y : (double?)null,
                i.Branch.GeoLocation != null ? i.Branch.GeoLocation.X : (double?)null,
                (double)i.Branch.ServiceRadiusKm,
                i.Branch.SupportsDelivery,
                i.Branch.SupportsPickup,
                i.DrugId,
                i.StockQuantity - i.ReservedQuantity,
                i.UnitPrice,
                i.Branch.WorkingSchedule
                    .Where(s => s.Day == today)
                    .Select(s => new ScheduleInfo(s.IsClosed, s.CloseTime))
                    .FirstOrDefault()))
            .ToListAsync(cancellationToken);

        var requestedCount = drugIds.Count;

        var branchGroups = stockRows.GroupBy(r => r.BranchId).ToList();
        var coords = new List<(double Lat, double Lon)> { (patientLocation.Latitude, patientLocation.Longitude) };
        var branchCoordIndex = new Dictionary<Guid, int>();

        foreach (var branchGroup in branchGroups)
        {
            var first = branchGroup.First();

            if (first.TodaySchedule != null)
            {
                var schedule = first.TodaySchedule;
                if (schedule.IsClosed)
                {
                    logger.LogDebug(
                        "PharmacyInventoryPlugin — Branch {BranchId} Pre-filtered: closed today.",
                        first.BranchId);
                    continue;
                }

                if (schedule.CloseTime.HasValue)
                {
                    var cairoTz = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
                    var nowCairo = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, cairoTz);
                    var nowTime = TimeOnly.FromDateTime(nowCairo);

                    if (nowTime >= schedule.CloseTime.Value)
                    {
                        logger.LogDebug(
                            "PharmacyInventoryPlugin — Branch {BranchId} Pre-filtered: past closing time ({Time}).",
                            first.BranchId, schedule.CloseTime.Value);
                        continue;
                    }
                }
            }

            if (first.Latitude is { } lat && first.Longitude is { } lng)
            {
                var straightLineKm = CalculateHaversineDistance(patientLocation.Latitude, patientLocation.Longitude, lat, lng);
                if (straightLineKm > MaxDistanceKm)
                {
                    logger.LogDebug(
                        "PharmacyInventoryPlugin — Branch {BranchId} Pre-filtered: straight-line distance {Dist:F2}km > {MaxKm}km",
                        first.BranchId, straightLineKm, MaxDistanceKm);
                    continue;
                }

                branchCoordIndex[first.BranchId] = coords.Count;
                coords.Add((lat, lng));
            }
        }

        var matrix = await osrmRoutingService.GetDistanceMatrixAsync(coords, cancellationToken);
        if (!matrix.IsSuccess)
        {
            logger.LogWarning("PharmacyInventoryPlugin — OSRM /table failed: {Msg}. Falling back to MaxValue distances.", matrix.Message);
        }

        LastMatrixResult = matrix;
        LastBranchCoordIndex = branchCoordIndex;

        //if (matrix.IsSuccess && matrix.DistancesKm != null)
        //{
        //    var sb = new System.Text.StringBuilder();
        //    sb.AppendLine($"OSRM Matrix Result: {coords.Count} points (Patient + {coords.Count - 1} branches).");
        //    sb.AppendLine("Patient to Branches Distances:");
            
        //    var branchIds = branchCoordIndex.Keys.ToList();
        //    foreach (var bId in branchIds)
        //    {
        //        var idx = branchCoordIndex[bId];
        //        sb.AppendLine($"  - Patient -> Branch {bId}: {matrix.DistancesKm[0][idx]:F2} km");
        //    }
            
        //    sb.AppendLine("Branch to Branch Distances:");
        //    for (int i = 0; i < branchIds.Count; i++)
        //    {
        //        var idx1 = branchCoordIndex[branchIds[i]];
        //        for (int j = i + 1; j < branchIds.Count; j++)
        //        {
        //            var idx2 = branchCoordIndex[branchIds[j]];
        //            sb.AppendLine($"  - Branch {branchIds[i]} <-> Branch {branchIds[j]}: {matrix.DistancesKm[idx1][idx2]:F2} km");
        //        }
        //    }
        //    logger.LogInformation("PharmacyInventoryPlugin — OSRM Call Summary:\n{Summary}", sb.ToString());
        //}

        var evaluations = new List<BranchFulfillmentEvaluation>();

        foreach (var branchGroup in branchGroups)
        {
            var first = branchGroup.First();
            var stockByDrug = branchGroup.ToDictionary(r => r.DrugId, r => r);

            var available = new List<AvailableItem>();
            var missing = new List<MissingItem>();

            foreach (var drugId in drugIds)
            {
                var needed = quantityByDrug[drugId];
                var (nameEn, nameAr) = NamesFor(drugId);

                if (stockByDrug.TryGetValue(drugId, out var row) && row.AvailableStock >= needed)
                    available.Add(new AvailableItem(drugId, nameEn, nameAr, needed, row.AvailableStock, row.UnitPrice));
                else
                    missing.Add(new MissingItem(
                        drugId, nameEn, nameAr, needed,
                        stockByDrug.TryGetValue(drugId, out var partial) ? partial.AvailableStock : 0));
            }

            var distanceKm = double.MaxValue;
            if (matrix.IsSuccess && branchCoordIndex.TryGetValue(first.BranchId, out var branchIdx))
            {
                distanceKm = matrix.DistancesKm[0][branchIdx];
            }

            if (distanceKm > MaxDistanceKm)
            {
                logger.LogDebug(
                    "PharmacyInventoryPlugin — Branch {BranchId} excluded: distance {DistanceKm}km > allowed {MaxKm}km (mode={Mode}).",
                    first.BranchId, distanceKm, MaxDistanceKm, fulfillmentMode);
                continue;
            }

            if (first.TodaySchedule != null)
            {
                var schedule = first.TodaySchedule;
                var durationMin = double.MaxValue;
                if (matrix.IsSuccess && matrix.DurationsMinutes.Length > 0 && branchCoordIndex.TryGetValue(first.BranchId, out var idx))
                {
                    durationMin = matrix.DurationsMinutes[0][idx];
                }

                if (durationMin < double.MaxValue && schedule.CloseTime.HasValue)
                {
                    var estimatedArrival = DateTime.UtcNow.AddMinutes(durationMin + 10);
                    var todayClose = DateTime.UtcNow.Date.Add(schedule.CloseTime.Value.ToTimeSpan());

                    if (estimatedArrival >= todayClose)
                    {
                        logger.LogDebug(
                            "PharmacyInventoryPlugin — Branch {BranchId} excluded: ETA {ETA:HH:mm} >= close time {Close:HH:mm} (travel {TravelMin}min + 10min prep).",
                            first.BranchId, estimatedArrival, todayClose, (int)durationMin);
                        continue;
                    }
                }
            }

            evaluations.Add(new BranchFulfillmentEvaluation
            {
                PharmacyId = first.PharmacyId,
                PharmacyName = first.PharmacyName,
                BranchId = first.BranchId,
                BranchName = first.BranchName,
                AvailableItemsCount = available.Count,
                RequestedItemsCount = requestedCount,
                AvailableItems = available,
                MissingItems = missing,
                DistanceKm = distanceKm,
                Latitude = first.Latitude,
                Longitude = first.Longitude,
                ServiceRadiusKm = first.ServiceRadiusKm,
                SupportsDelivery = first.SupportsDelivery,
                SupportsPickup = first.SupportsPickup
            });
        }

        evaluations = evaluations
            .OrderBy(e => e.DistanceKm)
            .ToList();

        const int MaxBranchesPerDrug = 4;
        var drugQuota = drugIds.ToDictionary(d => d, _ => MaxBranchesPerDrug);
        var finalCandidates = new HashSet<BranchFulfillmentEvaluation>();

        foreach (var eval in evaluations)
        {
            bool useful = false;
            foreach (var item in eval.AvailableItems)
            {
                if (drugQuota.TryGetValue(item.DrugId, out int remaining) && remaining > 0)
                {
                    useful = true;
                    drugQuota[item.DrugId] = remaining - 1;
                }
            }

            if (useful)
            {
                finalCandidates.Add(eval);
            }

            if (drugQuota.Values.All(v => v == 0))
            {
                break;
            }
        }

        evaluations = evaluations.Where(e => finalCandidates.Contains(e)).ToList();

        logger.LogDebug(
            "PharmacyInventoryPlugin.EvaluateAsync produced {BranchCount} branch evaluations (capped using {Max} branches per drug quota)",
            evaluations.Count, MaxBranchesPerDrug);

        return evaluations;
    }

    private static IReadOnlyList<CartItemDto> DeserializeCart(string cartItemsJson)
    {
        if (string.IsNullOrWhiteSpace(cartItemsJson))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<CartItemDto>>(cartItemsJson, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private sealed record StockRow(
        Guid PharmacyId,
        string PharmacyName,
        Guid BranchId,
        string BranchName,
        double? Latitude,
        double? Longitude,
        double ServiceRadiusKm,
        bool SupportsDelivery,
        bool SupportsPickup,
        Guid DrugId,
        int AvailableStock,
        decimal UnitPrice,
        ScheduleInfo? TodaySchedule);

    private sealed record ScheduleInfo(bool IsClosed, TimeOnly? CloseTime);

    private sealed record DrugNames(string En, string Ar);

    private static double CalculateHaversineDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371;
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double ToRadians(double angle) => Math.PI * angle / 180.0;
}
