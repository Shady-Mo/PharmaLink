using System;
using System.Collections.Generic;
using System.Linq;
using Application.Services.OrderSplitting;
using Application.Services.OrderSplitting.Models;

namespace OrderSplittingTester
{
    public class BruteForceOrderSplittingAlgorithm : IOrderSplittingAlgorithm
    {
        public string AlgorithmName => "Brute Force (Absolute Optimal)";

        public SplittingResult Execute(SplittingContext context)
        {
            var items = context.PendingItems.ToList();
            var branches = context.CandidateBranches.ToList();

            var validBranchesPerItem = new List<List<CandidateBranch>>();
            foreach (var item in items)
            {
                var validBranches = branches.Where(b =>
                    b.AvailableStock.TryGetValue(item.DrugId, out int qty) && qty >= item.QuantityNeeded).ToList();

                if (!validBranches.Any())
                {
                    return new SplittingResult(new List<ItemAssignment>(), items.Select(i => i.OrderItemId).ToList());
                }
                validBranchesPerItem.Add(validBranches);
            }

            var allCombinations = GenerateCombinations(validBranchesPerItem);

            List<CandidateBranch> bestCombination = null;
            double minTotalDistance = double.MaxValue;

            foreach (var combination in allCombinations)
            {
                double currentDistance = combination
                    .GroupBy(b => b.BranchId)
                    .Select(g => g.First().DistanceKm)
                    .Sum();

                if (currentDistance < minTotalDistance)
                {
                    minTotalDistance = currentDistance;
                    bestCombination = combination;
                }
            }

            var assignments = new List<ItemAssignment>();
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var assignedBranch = bestCombination[i];

                assignments.Add(new ItemAssignment(
                    item.OrderItemId,
                    assignedBranch.BranchId,
                    item.DrugId,
                    item.QuantityNeeded,
                    new AssignmentDecision(AlgorithmName, 1, assignedBranch.DistanceKm, 0)
                ));
            }

            return new SplittingResult(assignments, new List<Guid>());
        }

        private List<List<CandidateBranch>> GenerateCombinations(List<List<CandidateBranch>> lists)
        {
            var result = new List<List<CandidateBranch>>();
            GenerateCombinationsRecursive(lists, 0, new List<CandidateBranch>(), result);
            return result;
        }

        private void GenerateCombinationsRecursive(List<List<CandidateBranch>> lists, int depth, List<CandidateBranch> current, List<List<CandidateBranch>> result)
        {
            if (depth == lists.Count)
            {
                result.Add(new List<CandidateBranch>(current));
                return;
            }

            foreach (var item in lists[depth])
            {
                current.Add(item);
                GenerateCombinationsRecursive(lists, depth + 1, current, result);
                current.RemoveAt(current.Count - 1);
            }
        }
    }
}
