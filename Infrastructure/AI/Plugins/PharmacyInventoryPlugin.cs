using System.ComponentModel;
using System.Text.Json;
using Application.DTOs.OrderRouting;
using Infrastructure.Services;
using Microsoft.SemanticKernel;

namespace Infrastructure.AI.Plugins;

public sealed class PharmacyInventoryPlugin(
    IServiceScopeFactory scopeFactory,
    IOsrmRoutingService osrmRoutingService,
    ILogger<PharmacyInventoryPlugin> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Hard distance cap (km) for PICKUP orders — the patient drives to the branch, so a branch is
    /// only a candidate if it is within this radius regardless of the branch's own delivery radius.
    /// </summary>
    private const double PickupMaxDistanceKm = 20.0;

    /// <summary>
    /// Fulfillment mode applied by the <c>evaluate_candidate_branches</c> kernel function when an
    /// agent invokes it as a tool (the LLM tool signature can't carry the order's mode). The
    /// orchestrator sets this on the per-agent plugin instance so the agent's tool call uses the
    /// same geographic filter as the strongly-typed <see cref="EvaluateAsync"/> entry point.
    /// Defaults to Delivery.
    /// </summary>
    public FulfillmentMode DefaultFulfillmentMode { get; init; } = FulfillmentMode.Delivery;



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
        // Use the mode the orchestrator configured for this agent instance (defaults to Delivery),
        // so an agent-driven tool call applies the exact same geographic filter as the pre-computed
        // authoritative evaluations for this order.
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

        // Authoritative bilingual drug names straight from the catalog (English brand name + Arabic
        // name) so the returned plan can drive a bilingual confirmation popup. Falls back to the
        // cart-supplied English name if a drug row is somehow missing.
        var drugNames = await db.Drugs
            .AsNoTracking()
            .Where(d => drugIds.Contains(d.DrugId))
            .Select(d => new { d.DrugId, d.BrandName, d.ArabicName })
            .ToDictionaryAsync(d => d.DrugId, d => new DrugNames(d.BrandName, d.ArabicName), cancellationToken);

        (string En, string Ar) NamesFor(Guid drugId) =>
            drugNames.TryGetValue(drugId, out var n)
                ? (string.IsNullOrWhiteSpace(n.En) ? cartNameByDrug.GetValueOrDefault(drugId, string.Empty) : n.En, n.Ar)
                : (cartNameByDrug.GetValueOrDefault(drugId, string.Empty), string.Empty);


        // Get today's day of week to filter by working schedule.
        var today = DateTime.UtcNow.DayOfWeek;

        var stockRows = await db.PharmacyInventories
            .AsNoTracking()
            .Where(i => drugIds.Contains(i.DrugId) && (i.StockQuantity - i.ReservedQuantity) > 0)
            .Select(i => new StockRow(
                i.Branch.PharmacyId,
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

        // Gather all branch coordinates for the distance-matrix request. Index 0 = patient; 1..n = branches.
        var branchGroups = stockRows.GroupBy(r => r.BranchId).ToList();
        var coords = new List<(double Lat, double Lon)> { (patientLocation.Latitude, patientLocation.Longitude) };
        var branchCoordIndex = new Dictionary<Guid, int>(); // branchId → matrix index

        foreach (var branchGroup in branchGroups)
        {
            var first = branchGroup.First();
            if (first.Latitude is { } lat && first.Longitude is { } lng)
            {
                branchCoordIndex[first.BranchId] = coords.Count;
                coords.Add((lat, lng));
            }
        }

        // ONE OSRM /table request computes patient→branch distances for all branches at once. This
        // replaces N individual point-to-point calls, cutting latency and OSRM load by ~10×–30×.
        var matrix = await osrmRoutingService.GetDistanceMatrixAsync(coords, cancellationToken);
        if (!matrix.IsSuccess)
        {
            logger.LogWarning("PharmacyInventoryPlugin — OSRM /table failed: {Msg}. Falling back to MaxValue distances.", matrix.Message);
        }

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

            // Distance from the pre-computed matrix (patient = index 0, branch = branchCoordIndex[...]).
            // If the matrix failed or the branch has no coordinates, distanceKm remains MaxValue so it
            // sorts last rather than being misrepresented as "closest".
            var distanceKm = double.MaxValue;
            if (matrix.IsSuccess && branchCoordIndex.TryGetValue(first.BranchId, out var branchIdx))
            {
                distanceKm = matrix.DistancesKm[0][branchIdx]; // row 0 = patient, column = branch
            }


            // Mode-aware geographic feasibility gate (mirrors the real order path so the preview and
            // the actual split never disagree on which branches are reachable):
            //   • Delivery — branch must deliver to the patient: distance <= the branch's ServiceRadiusKm.
            //   • Pickup   — patient drives to the branch: distance <= a fixed PickupMaxDistanceKm cap.
            // A branch with an unknown distance (OSRM failed / no coordinates) can't be proven in-range,
            // so it is excluded rather than risk surfacing an unreachable branch.
            var maxAllowedKm = fulfillmentMode == FulfillmentMode.Pickup
                ? PickupMaxDistanceKm
                : first.ServiceRadiusKm;

            if (distanceKm > maxAllowedKm)
            {
                logger.LogDebug(
                    "PharmacyInventoryPlugin — Branch {BranchId} excluded: distance {DistanceKm}km > allowed {MaxKm}km (mode={Mode}).",
                    first.BranchId, distanceKm, maxAllowedKm, fulfillmentMode);
                continue;
            }

            // Working-hours feasibility gate: if the branch would close before delivery arrival (ETA),
            // it is excluded. ETA = current time (UTC) + travel duration. For Pickup mode, skip this check
            // since the patient controls their own arrival time.
            if (fulfillmentMode == FulfillmentMode.Delivery && first.TodaySchedule != null)
            {
                var schedule = first.TodaySchedule;
                if (schedule.IsClosed)
                {
                    logger.LogDebug(
                        "PharmacyInventoryPlugin — Branch {BranchId} excluded: closed today.",
                        first.BranchId);
                    continue;
                }

                // Duration from the matrix (patient→branch in minutes). Add a 10-minute prep buffer
                // for the pharmacy to prepare the order before the driver leaves.
                var durationMin = double.MaxValue;
                if (matrix.IsSuccess && matrix.DurationsMinutes.Length > 0 && branchCoordIndex.TryGetValue(first.BranchId, out var idx))
                {
                    durationMin = matrix.DurationsMinutes[0][idx];
                }

                if (durationMin < double.MaxValue && schedule.CloseTime.HasValue)
                {
                    var estimatedArrival = DateTime.UtcNow.AddMinutes(durationMin + 10); // +10min prep buffer
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

        // Primary: coverage desc (fewest splits). Secondary: distance asc.
        evaluations = evaluations
            .OrderByDescending(e => e.AvailableItemsCount)
            .ThenBy(e => e.DistanceKm)
            .ToList();

        // Performance cap: hand the AI at most 20 candidate branches so its reasoning stays fast and
        // cheap. Naively taking the top-20 could drop a branch that is the ONLY source of a rare drug
        // (low overall coverage + far away), which would make the whole cart look unfulfillable. So
        // we take the best 20 by coverage/proximity, then guarantee the set still covers every drug
        // that is stocked *somewhere* — pulling in the closest covering branch for any drug the top
        // slice missed. This keeps the AI's input small while never sacrificing fulfillability.
        const int MaxCandidateBranches = 20;
        if (evaluations.Count > MaxCandidateBranches)
        {
            var capped = evaluations.Take(MaxCandidateBranches).ToList();

            var coveredDrugs = capped
                .SelectMany(e => e.AvailableItems.Select(a => a.DrugId))
                .ToHashSet();

            var drugsStockedSomewhere = evaluations
                .SelectMany(e => e.AvailableItems.Select(a => a.DrugId))
                .ToHashSet();

            var uncovered = drugsStockedSomewhere.Except(coveredDrugs).ToHashSet();
            if (uncovered.Count > 0)
            {
                // For each drug the top-20 missed, pull in the nearest branch that stocks it. Evaluations
                // are already sorted by coverage then distance, so First() here is the best such branch.
                var rescued = evaluations
                    .Skip(MaxCandidateBranches)
                    .Where(e => e.AvailableItems.Any(a => uncovered.Contains(a.DrugId)))
                    .Where(e => !capped.Contains(e));

                foreach (var branch in rescued)
                {
                    if (uncovered.Count == 0) break;
                    capped.Add(branch);
                    foreach (var a in branch.AvailableItems)
                        uncovered.Remove(a.DrugId);
                }
            }

            evaluations = capped;
        }

        logger.LogDebug(
            "PharmacyInventoryPlugin.EvaluateAsync produced {BranchCount} branch evaluations (capped ~{Cap} for AI performance, coverage preserved)",
            evaluations.Count, MaxCandidateBranches);

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

    /// <summary>Today's working-hours snapshot for a branch (closed flag + closing time).</summary>
    private sealed record ScheduleInfo(bool IsClosed, TimeOnly? CloseTime);

    /// <summary>Bilingual catalog names for a drug (English brand name + Arabic name).</summary>
    private sealed record DrugNames(string En, string Ar);

}


