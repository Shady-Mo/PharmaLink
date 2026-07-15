namespace Application.Services.OrderSplitting.Models;

public record SplittingResult(
    IReadOnlyList<ItemAssignment> Assignments,
    IReadOnlyList<Guid> UnassignedItemIds
);

public record ItemAssignment(
    Guid OrderItemId, 
    Guid BranchId, 
    Guid DrugId, 
    int QuantityNeeded,
    AssignmentDecision Decision
);

public record AssignmentDecision(
    string Strategy,
    int Coverage,
    double DistanceKm,
    int RemainingStock
);
