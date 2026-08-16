using System;
using System.Collections.Generic;
using System.Linq;
using Application.Services.OrderSplitting;
using Application.Services.OrderSplitting.Models;

namespace OrderSplittingTester
{
    public class BruteForceOrderSplittingAlgorithm : IOrderSplittingAlgorithm
    {
        private readonly double[][] _distanceMatrix;
        private readonly Dictionary<Guid, int> _branchIndexMap;

        public BruteForceOrderSplittingAlgorithm(double[][] distanceMatrix, Dictionary<Guid, int> branchIndexMap)
        {
            _distanceMatrix = distanceMatrix;
            _branchIndexMap = branchIndexMap;
        }

        public string AlgorithmName => "Brute Force (Absolute Optimal TSP)";

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
                var uniqueBranches = combination.GroupBy(b => b.BranchId).Select(g => g.First()).ToList();
                
                // Calculate TSP trip distance
                double tripDistance = CalculateShortestTrip(uniqueBranches);
                
                if (tripDistance < minTotalDistance)
                {
                    minTotalDistance = tripDistance;
                    bestCombination = combination;
                }
                else if (Math.Abs(tripDistance - minTotalDistance) < 0.001)
                {
                    if (bestCombination != null)
                    {
                        var bestUniqueCount = bestCombination.GroupBy(b => b.BranchId).Count();
                        if (uniqueBranches.Count < bestUniqueCount)
                        {
                            bestCombination = combination;
                        }
                    }
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
                    new AssignmentDecision(AlgorithmName, bestCombination.GroupBy(b => b.BranchId).Count(), minTotalDistance, 0)
                ));
            }

            return new SplittingResult(assignments, new List<Guid>());
        }

        private double CalculateShortestTrip(List<CandidateBranch> branches)
        {
            var indices = branches.Select(b => _branchIndexMap[b.BranchId]).ToList();
            var permutations = GetPermutations(indices, indices.Count);
            
            double minTrip = double.MaxValue;
            foreach (var perm in permutations)
            {
                var list = perm.ToList();
                double trip = 0;
                int current = 0;
                foreach (var next in list)
                {
                    trip += _distanceMatrix[current][next];
                    current = next;
                }
                if (trip < minTrip) minTrip = trip;
            }
            return minTrip;
        }

        private IEnumerable<IEnumerable<T>> GetPermutations<T>(IEnumerable<T> list, int length)
        {
            if (length == 1) return list.Select(t => new T[] { t });
            return GetPermutations(list, length - 1)
                .SelectMany(t => list.Where(e => !t.Contains(e)),
                    (t1, t2) => t1.Concat(new T[] { t2 }));
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
