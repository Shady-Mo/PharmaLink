using Application.Services.OrderSplitting.Models;

namespace Infrastructure.Services;

public class GreedyOrderSplittingAlgorithm : IOrderSplittingAlgorithm
{
    public string AlgorithmName => "Greedy Coverage-Then-Distance (Dynamic)";

    public SplittingResult Execute(SplittingContext context)
    {
        var assignments = new List<ItemAssignment>();

        var ledger = new Dictionary<(Guid, Guid), int>();
        foreach (var branch in context.CandidateBranches)
        {
            foreach (var stock in branch.AvailableStock)
            {
                ledger[(branch.BranchId, stock.Key)] = stock.Value;
            }
        }

        var remainingItems = context.PendingItems.ToList();


        while (remainingItems.Any())
        {
            // Evaluate coverage dynamically for the *remaining* items
            var bestBranch = context.CandidateBranches
                .Select(b => new
                {
                    Branch = b,
                    Coverage = remainingItems.Count(item =>
                        ledger.TryGetValue((b.BranchId, item.DrugId), out int available) &&
                        available >= item.QuantityNeeded)
                })
                .Where(x => x.Coverage > 0)
                .OrderByDescending(x => x.Coverage)
                .ThenBy(x => x.Branch.DistanceKm)
                .ThenBy(x => x.Branch.BranchId)
                .FirstOrDefault();

            // If no branch can fulfill any of the remaining items, we are stuck.
            if (bestBranch is null) break;

            var branchId = bestBranch.Branch.BranchId;

            // Fulfill as many remaining items as possible from this best branch
            var itemsToRemove = new List<PendingItem>();

            foreach (var item in remainingItems)
            {
                if (ledger.TryGetValue((branchId, item.DrugId), out int available) &&
                    available >= item.QuantityNeeded)
                {
                    var decision = new AssignmentDecision(
                        Strategy: AlgorithmName,
                        Coverage: bestBranch.Coverage,
                        DistanceKm: bestBranch.Branch.DistanceKm,
                        RemainingStock: available - item.QuantityNeeded
                    );

                    assignments.Add(new ItemAssignment(
                        item.OrderItemId,
                        branchId,
                        item.DrugId,
                        item.QuantityNeeded,
                        decision));

                    // Deduct from ledger
                    ledger[(branchId, item.DrugId)] -= item.QuantityNeeded;
                    itemsToRemove.Add(item);
                }
            }

            foreach (var item in itemsToRemove)
            {
                remainingItems.Remove(item);
            }
        }

        var unassignedItemIds = remainingItems.Select(i => i.OrderItemId).ToList();

        return new SplittingResult(assignments, unassignedItemIds);
    }
}