namespace Application.DTOs.OrderRouting;

public sealed record OrderRoutingPlan
{
    public string Strategy { get; init; } = string.Empty;

    public IReadOnlyList<OrderFulfillmentLegPlan> Legs { get; init; } = [];

    public IReadOnlyList<MissingItem> UnfulfillableItems { get; init; } = [];

    public int FulfillmentLegCount => Legs.Count;

    public double TotalDistanceKm { get; init; }

    public bool IsFullyFulfilled => UnfulfillableItems.Count == 0;

    public string Reasoning { get; init; } = string.Empty;

    public RouteSummary? RouteSummary { get; init; }
}

public sealed record RouteSummary
{
    public IReadOnlyList<RouteStop> Stops { get; init; } = [];

    public double TotalDistanceKm { get; init; }

    public string OptimizedBy { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;
}

public sealed record RouteStop
{
    public int Order { get; init; }

    public Guid BranchId { get; init; }

    public string PharmacyName { get; init; } = string.Empty;

    public string BranchName { get; init; } = string.Empty;

    public double DistanceFromPreviousKm { get; init; }

    public IReadOnlyList<string> ItemsToCollect { get; init; } = [];
}


public sealed record OrderFulfillmentLegPlan
{
    public Guid PharmacyId { get; init; }

    public Guid BranchId { get; init; }

    public string BranchName { get; init; } = string.Empty;

    public double DistanceKm { get; init; }

    public IReadOnlyList<FulfilledLineItem> Items { get; init; } = [];

    public decimal LegSubtotal { get; init; }
}

public sealed record FulfilledLineItem(

    Guid DrugId,
    string DrugName,
    string DrugNameAr,
    int Quantity,
    decimal UnitPrice)
{
    public decimal LineTotal => UnitPrice * Quantity;
}


