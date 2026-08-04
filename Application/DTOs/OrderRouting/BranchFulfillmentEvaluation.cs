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

    public double ServiceRadiusKm { get; init; }

    public bool SupportsDelivery { get; init; }

    public bool SupportsPickup { get; init; }

    public bool CoversEntireCart => RequestedItemsCount > 0 && AvailableItemsCount == RequestedItemsCount;

    public bool WithinServiceRadius => DistanceKm <= ServiceRadiusKm;
}

public sealed record MissingItem(Guid DrugId, string DrugName, int QuantityNeeded, int QuantityAvailable);

public sealed record AvailableItem(Guid DrugId, string DrugName, int QuantityNeeded, int QuantityAvailable, decimal UnitPrice);
