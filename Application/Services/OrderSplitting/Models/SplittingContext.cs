namespace Application.Services.OrderSplitting.Models;

using Domain.Enums;

public record SplittingContext(
    Guid OrderId,
    FulfillmentMode FulfillmentMode,
    IReadOnlyList<PendingItem> PendingItems,
    IReadOnlyList<CandidateBranch> CandidateBranches
);

public record PendingItem(Guid OrderItemId, Guid DrugId, int QuantityNeeded);

public record CandidateBranch(
    Guid BranchId,
    string BranchName,
    double DistanceKm,
    bool SupportsDelivery,
    bool SupportsPickup,
    IReadOnlyDictionary<Guid, int> AvailableStock
);
