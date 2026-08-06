namespace Application.DTOs.OrderRouting;

public sealed record BranchFulfillmentEvaluation
{
    public Guid PharmacyId { get; init; }

    public Guid BranchId { get; init; }

    public string BranchName { get; init; } = string.Empty;

    public int AvailableItemsCount { get; init; }

    public int RequestedItemsCount { get; init; }

    public IReadOnlyList<MissingItem> MissingItems { get; init; } = [];

    public IReadOnlyList<AvailableItem> AvailableItems { get; init; } = [];

    public double DistanceKm { get; init; }

    /// <summary>Branch latitude (decimal degrees). Needed to compute branch→branch trip distances.</summary>
    public double? Latitude { get; init; }

    /// <summary>Branch longitude (decimal degrees). Needed to compute branch→branch trip distances.</summary>
    public double? Longitude { get; init; }

    public double ServiceRadiusKm { get; init; }


    public bool SupportsDelivery { get; init; }

    public bool SupportsPickup { get; init; }

    public bool CoversEntireCart => RequestedItemsCount > 0 && AvailableItemsCount == RequestedItemsCount;

    public bool WithinServiceRadius => DistanceKm <= ServiceRadiusKm;
}

/// <summary>
/// A requested cart item a branch cannot (fully) supply. <c>DrugName</c> is the English brand
/// name and <c>DrugNameAr</c> is the Arabic name (both for the confirmation popup).
/// </summary>
public sealed record MissingItem(
    Guid DrugId,
    string DrugName,
    string DrugNameAr,
    int QuantityNeeded,
    int QuantityAvailable);

/// <summary>
/// A requested cart item a branch can supply. <c>DrugName</c> is the English brand name and
/// <c>DrugNameAr</c> is the Arabic name (both for the confirmation popup).
/// </summary>
public sealed record AvailableItem(

    Guid DrugId,
    string DrugName,
    string DrugNameAr,
    int QuantityNeeded,
    int QuantityAvailable,
    decimal UnitPrice);


