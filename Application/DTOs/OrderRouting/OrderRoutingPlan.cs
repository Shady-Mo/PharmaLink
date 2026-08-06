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

    /// <summary>
    /// Human/driver-facing turn-by-turn summary of the optimal pickup route: the ordered list of
    /// stops (which branch to visit first, next, ...) plus a short readable text. Ordering is the
    /// AI's chosen visiting order, or — when the AI is unavailable (quota / null response) — the
    /// exact optimal order computed by the Held-Karp TSP fallback.
    /// </summary>
    public RouteSummary? RouteSummary { get; init; }
}

/// <summary>
/// Ordered, driver-facing route: the sequence of branch stops starting from the patient location,
/// each with its leg items, plus a readable one-line description ("Go to A first, then B, ...").
/// </summary>
public sealed record RouteSummary
{
    /// <summary>Ordered stops. Stop 1 is the first branch to visit after leaving the patient.</summary>
    public IReadOnlyList<RouteStop> Stops { get; init; } = [];

    /// <summary>Total driving distance (km) of the whole trip in the given stop order.</summary>
    public double TotalDistanceKm { get; init; }

    /// <summary>How the order was decided: "AI-MultiAgent" or "Held-Karp (TSP fallback)".</summary>
    public string OptimizedBy { get; init; } = string.Empty;

    /// <summary>Readable, ordered description of the trip.</summary>
    public string Description { get; init; } = string.Empty;
}

/// <summary>A single ordered stop on the pickup route.</summary>
public sealed record RouteStop
{
    /// <summary>1-based visiting order.</summary>
    public int Order { get; init; }

    public Guid BranchId { get; init; }

    public string BranchName { get; init; } = string.Empty;

    /// <summary>Driving distance (km) from the previous point (patient for stop 1) to this stop.</summary>
    public double DistanceFromPreviousKm { get; init; }

    /// <summary>Drug names to collect at this stop (for a quick glance in the summary).</summary>
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

/// <summary>
/// A fulfilled line in a branch leg. <c>DrugName</c> is the English brand name and
/// <c>DrugNameAr</c> is the Arabic name (both surfaced in the confirmation popup).
/// </summary>
public sealed record FulfilledLineItem(

    Guid DrugId,
    string DrugName,
    string DrugNameAr,
    int Quantity,
    decimal UnitPrice)
{
    public decimal LineTotal => UnitPrice * Quantity;
}


