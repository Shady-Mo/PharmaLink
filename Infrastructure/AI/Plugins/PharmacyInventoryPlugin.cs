using System.ComponentModel;
using System.Text.Json;
using Application.DTOs.OrderRouting;
using Microsoft.SemanticKernel;

namespace Infrastructure.AI.Plugins;

public sealed class PharmacyInventoryPlugin(
    IServiceScopeFactory scopeFactory,
    ILogger<PharmacyInventoryPlugin> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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
            new GeoLocation(patientLatitude, patientLongitude), cartItems, cancellationToken);

        return JsonSerializer.Serialize(evaluations, JsonOptions);
    }

    public async Task<IReadOnlyList<BranchFulfillmentEvaluation>> EvaluateAsync(
        GeoLocation patientLocation,
        IReadOnlyList<CartItemDto> cartItems,
        CancellationToken cancellationToken = default)
    {
        if (cartItems.Count == 0)
            return [];

        logger.LogInformation(
            "PharmacyInventoryPlugin.EvaluateAsync — {ItemCount} cart items from ({Lat},{Lng})",
            cartItems.Count, patientLocation.Latitude, patientLocation.Longitude);

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var drugIds = cartItems.Select(c => c.DrugId).Distinct().ToList();
        var quantityByDrug = cartItems
            .GroupBy(c => c.DrugId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));
        var nameByDrug = cartItems
            .GroupBy(c => c.DrugId)
            .ToDictionary(g => g.Key, g => g.First().DrugName);

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
                i.UnitPrice))
            .ToListAsync(cancellationToken);

        var requestedCount = drugIds.Count;

        var evaluations = stockRows
            .GroupBy(r => r.BranchId)
            .Select(branchGroup =>
            {
                var first = branchGroup.First();
                var stockByDrug = branchGroup.ToDictionary(r => r.DrugId, r => r);

                var available = new List<AvailableItem>();
                var missing = new List<MissingItem>();

                foreach (var drugId in drugIds)
                {
                    var needed = quantityByDrug[drugId];
                    var name = nameByDrug[drugId];

                    if (stockByDrug.TryGetValue(drugId, out var row) && row.AvailableStock >= needed)
                        available.Add(new AvailableItem(drugId, name, needed, row.AvailableStock, row.UnitPrice));
                    else
                        missing.Add(new MissingItem(
                            drugId, name, needed,
                            stockByDrug.TryGetValue(drugId, out var partial) ? partial.AvailableStock : 0));
                }

                var distanceKm = first.Latitude is { } lat && first.Longitude is { } lng
                    ? Math.Round(GeoDistancePlugin.Haversine(
                        patientLocation.Latitude, patientLocation.Longitude, lat, lng), 3)
                    : double.MaxValue;

                return new BranchFulfillmentEvaluation
                {
                    PharmacyId = first.PharmacyId,
                    BranchId = first.BranchId,
                    BranchName = first.BranchName,
                    AvailableItemsCount = available.Count,
                    RequestedItemsCount = requestedCount,
                    AvailableItems = available,
                    MissingItems = missing,
                    DistanceKm = distanceKm,
                    ServiceRadiusKm = first.ServiceRadiusKm,
                    SupportsDelivery = first.SupportsDelivery,
                    SupportsPickup = first.SupportsPickup
                };
            })
            // Primary: coverage desc (fewest splits). Secondary: distance asc.
            .OrderByDescending(e => e.AvailableItemsCount)
            .ThenBy(e => e.DistanceKm)
            .ToList();

        logger.LogDebug(
            "PharmacyInventoryPlugin.EvaluateAsync produced {BranchCount} branch evaluations",
            evaluations.Count);

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
        decimal UnitPrice);
}
