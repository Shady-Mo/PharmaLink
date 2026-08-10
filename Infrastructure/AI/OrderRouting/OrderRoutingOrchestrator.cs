using System.Text.Json;
using System.Text.Json.Serialization;
using Application.DTOs.OrderRouting;
using Application.Services.OrderRouting;
using Application.Services.OrderSplitting.Models;
using Infrastructure.AI;
using Infrastructure.AI.Abstractions;
using Infrastructure.AI.Models;
using Infrastructure.AI.Plugins;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Infrastructure.AI.OrderRouting;

#pragma warning disable SKEXP0001, SKEXP0110

public sealed class OrderRoutingOrchestrator : IOrderRoutingOrchestrator
{
    private readonly IKernelProvider _kernelProvider;
    private readonly PharmacyInventoryPlugin _inventoryPlugin;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOsrmRoutingService _osrmRoutingService;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<OrderRoutingOrchestrator> _logger;

    public static readonly int MAX_TOKENS = 1200;

    public const int MAX_CLUSTER_BRANCHES = 6;


    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public OrderRoutingOrchestrator(
        IEnumerable<IKernelProvider> kernelProviders,
        PharmacyInventoryPlugin inventoryPlugin,
        IServiceScopeFactory scopeFactory,
        IOsrmRoutingService osrmRoutingService,
        ILoggerFactory loggerFactory,
        ILogger<OrderRoutingOrchestrator> logger)
    {
        _kernelProvider = kernelProviders.FirstOrDefault(p => p.Provider == AIProvider.ITIOrderSplitting)
            ?? throw new InvalidOperationException(
                "ITIOrderSplitting provider not registered. Ensure ITIOrderSplittingProvider is " +
                "registered in DI and AI:Providers:ITIOrderSplitting is configured in appsettings.json.");
        _inventoryPlugin = inventoryPlugin;
        _scopeFactory = scopeFactory;
        _osrmRoutingService = osrmRoutingService;
        _loggerFactory = loggerFactory;
        _logger = logger;
    }

    public async Task<OrderRoutingPlan> OptimizeOrderFulfillmentAsync(
        Guid patientUserId,
        GeoLocation patientLocation,
        IReadOnlyList<CartItemDto> cartItems,
        FulfillmentMode fulfillmentMode,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "OrderRoutingOrchestrator — Optimizing fulfillment for Patient {PatientId}, {ItemCount} cart items, mode={Mode}",
            patientUserId, cartItems.Count, fulfillmentMode);

        if (cartItems.Count == 0)
            return EmptyPlan("Cart is empty; nothing to route.");

        var evaluations = await _inventoryPlugin.EvaluateAsync(patientLocation, cartItems, fulfillmentMode, cancellationToken);

        _logger.LogInformation(
            "OrderRoutingOrchestrator — Inventory evaluation found {Count} candidate branch(es) for mode={Mode}.",
            evaluations.Count, fulfillmentMode);

        if (evaluations.Count == 0)
        {
            _logger.LogWarning(
                "OrderRoutingOrchestrator — NO in-range branch stocks any cart item (patient=({Lat},{Lon}), mode={Mode}). " +
                "Returning NothingAvailablePlan — the AI clustering step (and its cluster logs) is SKIPPED.",
                patientLocation.Latitude, patientLocation.Longitude, fulfillmentMode);
            return NothingAvailablePlan(cartItems);
        }

        var stockedDrugs = evaluations
            .SelectMany(e => e.AvailableItems.Select(a => a.DrugId))
            .ToHashSet();
        var routableCart = cartItems.Where(c => stockedDrugs.Contains(c.DrugId)).ToList();
        var unstockedItems = cartItems.Where(c => !stockedDrugs.Contains(c.DrugId)).ToList();

        if (unstockedItems.Count > 0)
            _logger.LogWarning(
                "OrderRoutingOrchestrator — {Count} cart drug(s) are stocked by NO in-range branch; they will be " +
                "reported UNFULFILLABLE and are deliberately NOT sent to the AI (prevents the coverage-rule " +
                "hallucination / token exhaustion): [{Drugs}]",
                unstockedItems.Count, string.Join(", ", unstockedItems.Select(i => i.DrugId)));

        if (routableCart.Count == 0)
        {
            _logger.LogWarning(
                "OrderRoutingOrchestrator — No cart drug is stocked by any in-range branch; returning NothingAvailablePlan.");
            return NothingAvailablePlan(cartItems);
        }

        RouterDecision? decision = null;
        double[][]? globalMatrix = null;
        try
        {
            (decision, globalMatrix) = await RunAgentDecisionAsync(patientLocation, routableCart, evaluations, fulfillmentMode, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "OrderRoutingOrchestrator — Agent decision failed; falling back to deterministic heuristic.");
        }

        OrderRoutingPlan? plan = null;
        if (decision?.Clusters is { Count: > 0 })
        {
            plan = SelectBestCluster(decision, globalMatrix, evaluations, routableCart);
            if (plan is null)
                _logger.LogWarning(
                    "OrderRoutingOrchestrator — AI FALLBACK CAUSE #3: {ClusterCount} cluster(s) parsed but NONE " +
                    "produced usable legs after validation (hallucinated branchId(s)/drugId(s)). Falling back to Held-Karp.",
                    decision.Clusters!.Count);
        }

        else if (decision is not null)
        {
            _logger.LogWarning(
                "OrderRoutingOrchestrator — AI FALLBACK CAUSE #2: RouterDecision parsed but contained no clusters. " +
                "Falling back to Held-Karp.");
        }
        else
        {
            _logger.LogWarning(
                "OrderRoutingOrchestrator — AI FALLBACK CAUSE #1: no parseable RouterDecision returned " +
                "(router produced no final JSON message, hit quota, or output was unparseable). Falling back to Held-Karp.");
        }

        var producedByAi = plan is not null && plan.Legs.Count > 0;

        if (!producedByAi)
        {
            _logger.LogInformation(
                "OrderRoutingOrchestrator — Agent returned no usable plan; running Held-Karp TSP fallback.");
            plan = BuildDeterministicPlan(evaluations, routableCart);
        }
        else
        {
            _logger.LogInformation(
                "OrderRoutingOrchestrator — AI winning cluster accepted: {LegCount} leg(s), tripKm={Trip}.",
                plan!.Legs.Count, plan.TotalDistanceKm);
        }

        plan = AppendUnstockedItems(plan!, unstockedItems, evaluations);

        try
        {
            var summary = await BuildRouteSummaryAsync(patientLocation, plan!, evaluations, producedByAi, fulfillmentMode, cancellationToken);

            if (summary is not null)
            {
                var hopByBranch = summary.Stops.ToDictionary(s => s.BranchId, s => s.DistanceFromPreviousKm);
                var updatedLegs = plan!.Legs
                    .Select(leg => hopByBranch.TryGetValue(leg.BranchId, out var hopKm)
                        ? leg with { DistanceKm = hopKm }
                        : leg)
                    .ToList();

                plan = plan with
                {
                    Legs = updatedLegs,
                    RouteSummary = summary,
                    TotalDistanceKm = summary.TotalDistanceKm
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OrderRoutingOrchestrator — Failed to build route summary; returning plan without it.");
        }

        _logger.LogInformation(
            "OrderRoutingOrchestrator — Plan '{Strategy}': {LegCount} leg(s), {TotalKm:F2} km trip, fullyFulfilled={Full}, optimizedBy={By}",
            plan!.Strategy, plan.FulfillmentLegCount, plan.TotalDistanceKm, plan.IsFullyFulfilled,
            producedByAi ? "AI-MultiAgent" : "Held-Karp");

        return plan;
    }

    private async Task<(RouterDecision? Decision, double[][]? Matrix)> RunAgentDecisionAsync(
        GeoLocation patientLocation,
        IReadOnlyList<CartItemDto> cartItems,
        IReadOnlyList<BranchFulfillmentEvaluation> evaluations,
        FulfillmentMode fulfillmentMode,
        CancellationToken cancellationToken)
    {
        var kernel = _kernelProvider.GetKernel(ModelRole.Chat).Clone();
        kernel.Plugins.Clear();

        var coords = new List<(double Lat, double Lon)> { (patientLocation.Latitude, patientLocation.Longitude) };
        coords.AddRange(evaluations.Select(e => (e.Latitude ?? 0d, e.Longitude ?? 0d)));

        var matrix = await _osrmRoutingService.GetDistanceMatrixAsync(coords, cancellationToken);
        var haveMatrix = matrix.IsSuccess && matrix.DistancesKm.Length == coords.Count;

        var slimBranches = evaluations.Select((e, i) => new
        {
            m = i + 1,
            e.BranchId,
            e.BranchName,
            coversEntireCart = e.CoversEntireCart,
            available = e.AvailableItems.Select(a => new { a.DrugId, qty = a.QuantityAvailable })
        });

        var cartJson = JsonSerializer.Serialize(
            cartItems.Select(c => new { c.DrugId, c.Quantity }), JsonOptions);
        var branchesJson = JsonSerializer.Serialize(slimBranches, JsonOptions);

        string distanceText;
        if (haveMatrix)
        {
            distanceText = FormatDistancesForPrompt(matrix.DistancesKm, evaluations);
        }
        else
        {
            _logger.LogWarning(
                "OrderRoutingOrchestrator — OSRM distance matrix unavailable ({Msg}); the router will optimize " +
                "on patient->branch distances only (no branch<->branch data).", matrix.Message);
            var sb2 = new StringBuilder();
            sb2.AppendLine("Patient → branches (nearest first):");
            var sorted = evaluations
                .Select((e, i) => (m: i + 1, km: Math.Round(e.DistanceKm, 2)))
                .OrderBy(x => x.km);
            foreach (var (m, km) in sorted)
                sb2.AppendLine($"  patient→m{m}: {km} km");
            sb2.AppendLine("(Branch↔branch distances unavailable — OSRM offline.)");
            distanceText = sb2.ToString();
        }

        var prompt =
            $$"""
            You are a pharmacy order-routing optimizer. PROPOSE several candidate CLUSTERS of branches that
            can each fulfil the WHOLE cart. The backend will measure every cluster's EXACT driving trip with
            an optimal TSP solver (Held-Karp) and pick the winner — so DO NOT compute trip distances yourself.
            Your ONLY job is to propose GOOD GROUPINGS based on coverage and relative proximity from the matrix.



            Cart items (JSON — each drugId with the quantity needed): {{cartJson}}

            Candidate branches. "m" is a branch label used in the distances section below;
            "coversEntireCart"=true means that single branch stocks every cart drug; "available" lists the
            DrugIds it can supply:
            {{branchesJson}}

            Driving distances (kilometres). Read these as plain statements — no indexing needed.
            UNREACHABLE pairs are omitted. Branch-to-branch pairs further than the median are also omitted
            (they are too far apart to be in the same cluster anyway).
            {{distanceText}}

            HOW TO BUILD GOOD CLUSTERS:
            - RULE 1 — COVERAGE (mandatory): the branches in a cluster must TOGETHER stock EVERY cart drug.
              Check this against each branch's "available" list. A cluster that misses any cart drug is invalid.

            - RULE 2 — GEOGRAPHIC COHESION (the most important rule for multi-branch clusters):
              When a cluster contains more than one branch, those branches MUST be geographically close to
              EACH OTHER AND close to the patient — read this directly from the distance statements above.
              * The trip visits: patient → branch_1 → branch_2 → ...
                A large branch↔branch distance makes that cluster BAD even if both branches are near the patient.
              * GOOD cluster: patient→m2: 2.1 km, m2↔m3: 0.8 km → m2 and m3 are close to each other ✅
              * BAD cluster:  patient→m1: 1.5 km, patient→m3: 1.8 km, m1↔m3: 13.0 km → m1 and m3 are
                              far apart even though both are individually close to the patient — AVOID ❌
              * To find the best partner for branch mX: look at all "mX↔mY" lines, pick the mY with the
                SMALLEST distance that also stocks the missing drug. That mY is the nearest neighbor of mX.

            - RULE 3 — NO POINTLESS DETOURS: NEVER add a branch with a large M[0][m] OR a large
              inter-branch M[a][b] to a cluster when a nearer alternative covers the same drug.

            - RULE 4 — MINIMALITY (NO REDUNDANT BRANCHES):
              * Every branch in a cluster MUST contribute AT LEAST ONE UNIQUE DRUG that is not already provided
                by the other branches in that same cluster.
              * Never add extra branches to a cluster if the cart is already 100% covered by a smaller subset.
              * EXAMPLE A: If the patient requests Drug A, Pharmacy 1 (stocks A) and Pharmacy 2 (stocks A) must
                              be returned as SEPARATE clusters: `[1]` and `[2]`. NEVER combine them into `[1, 2]`.
              * EXAMPLE B: If the patient requests Drug B and Drug D, and Pharmacy 1 stocks (A, B) while
                              Pharmacy 2 stocks (A, D), combine them into ONE cluster `[1, 2]` because BOTH are
                              required to complete the cart.
            - Prefer FEWER branches per cluster. If a branch has coversEntireCart=true, a single-branch cluster
              with just it is often the best choice. Never add extra branches when a smaller subset covers all.
            - Propose 4 to 6 GENUINELY DIFFERENT clusters so the backend has real choices to measure.
              Each cluster may contain at most {{MAX_CLUSTER_BRANCHES}} branches.
              Use ONLY the "m" indices shown above; never invent indices.

            KEEP THE OUTPUT SHORT so it is NEVER cut off by the token limit:
            - "reasoning" must be AT MOST 8 words. Do NOT include matrix numbers or DrugIds in reasoning.
            - Put "branches" FIRST in every cluster object, then "reasoning".

            Respond with ONLY this JSON (no prose, no markdown, no code fences). "branches" is the list of
            branch "m" indices that form the cluster:
            {
              "clusters": [
                { "branches": [<m>, ...], "estimatedTripKm": <number>, "reasoning": "<=8 words, no DrugIds or numbers" }
              ]
            }
            """;

        var chat = kernel.GetRequiredService<IChatCompletionService>();
        var history = new ChatHistory();
        history.AddUserMessage(prompt);

        var settings = new PromptExecutionSettings
        {
            ExtensionData = new Dictionary<string, object>
            {
                ["max_tokens"] = MAX_TOKENS,
                ["temperature"] = 0.3
            }
        };

        var response = await chat.GetChatMessageContentAsync(
            history, settings, kernel: kernel, cancellationToken: cancellationToken);

        var raw = response.Content;
        if (string.IsNullOrWhiteSpace(raw))
        {
            _logger.LogWarning(
                "OrderRoutingOrchestrator — AI FALLBACK CAUSE #1a: the router LLM returned an empty message.");
            return (null, haveMatrix ? matrix.DistancesKm : null);
        }

        _logger.LogInformation(
            "OrderRoutingOrchestrator — Raw router message ({Len} chars): {Raw}",
            raw.Length, raw.Length > 2000 ? raw[..2000] + "…(truncated)" : raw);

        return (ParseRouterDecision(raw), haveMatrix ? matrix.DistancesKm : null);
    }

    private OrderRoutingPlan? SelectBestCluster(
        RouterDecision decision,
        double[][]? globalDist,
        IReadOnlyList<BranchFulfillmentEvaluation> evaluations,
        IReadOnlyList<CartItemDto> cartItems)
    {
        if (globalDist is null)
        {
            _logger.LogWarning(
                "OrderRoutingOrchestrator — Global distance matrix unavailable; cannot score AI clusters. Falling back.");
            return null;
        }

        var cartDrugs = cartItems.Select(c => c.DrugId).ToHashSet();
        var validClusters = new List<List<int>>();
        var seen = new HashSet<string>(); // dedupe identical branch-sets
        var clusterSources = new Dictionary<string, string>();

        void TryAddCluster(IEnumerable<int> branchIndices, string source)
        {
            var idx = branchIndices
                .Where(m => m >= 1 && m <= evaluations.Count)
                .Distinct()
                .ToList();
            if (idx.Count == 0 || idx.Count > MAX_CLUSTER_BRANCHES)
                return;

            var covered = idx.SelectMany(m => evaluations[m - 1].AvailableItems.Select(a => a.DrugId)).ToHashSet();
            if (!cartDrugs.All(covered.Contains))
                return;

            var key = string.Join(",", idx.OrderBy(x => x));
            if (seen.Add(key))
            {
                validClusters.Add(idx);
                clusterSources[key] = source;
            }
        }

        foreach (var c in decision.Clusters!)
            if (c.Branches is { Count: > 0 })
                TryAddCluster(c.Branches, "AI");

        var nearestSingleCover = evaluations
            .Select((e, i) => (Eval: e, M: i + 1))
            .FirstOrDefault(x => x.Eval.CoversEntireCart);
        if (nearestSingleCover.Eval is not null)
            TryAddCluster(new[] { nearestSingleCover.M }, "Baseline:NearestSingleCover");

        var nearestPerDrug = new List<int>();
        var perDrugSeen = new HashSet<int>();
        foreach (var drug in cartDrugs)
        {
            for (int i = 0; i < evaluations.Count; i++)
            {
                if (evaluations[i].AvailableItems.Any(a => a.DrugId == drug))
                {
                    if (perDrugSeen.Add(i + 1))
                        nearestPerDrug.Add(i + 1);
                    break;
                }
            }
        }
        if (nearestPerDrug.Count > 0)
            TryAddCluster(nearestPerDrug, "Baseline:NearestPerDrug");

        /* Truthy Deterministic Calculation
        if (globalDist is not null) {
            const int KNearestNeighbors = 4;
            int branchCount = evaluations.Count;

            for (int a = 0; a < branchCount; a++) {
                // The K branches closest to branch `a` (by driving distance, excluding patient at 0).
                // globalDist indices: 0=patient, 1..n=branches (1-based "m" = index+1).
                var nearestToA = Enumerable.Range(0, branchCount)
                    .Where(b => b != a && globalDist[a + 1][b + 1] >= 0) // skip unreachable (-1)
                    .OrderBy(b => globalDist[a + 1][b + 1])
                    .Take(KNearestNeighbors)
                    .ToList();

                foreach (var b in nearestToA) {
                    // --- Pair [a+1, b+1] ---
                    TryAddCluster(new[] { a + 1, b + 1 }, "Baseline:ProximityPair");

                    // If the pair doesn't cover the cart, try extending it with a third branch
                    // that is close to EITHER a or b (take the closer hop for each candidate).
                    var pairCovered = evaluations[a].AvailableItems.Select(x => x.DrugId)
                        .Union(evaluations[b].AvailableItems.Select(x => x.DrugId))
                        .ToHashSet();

                    if (!cartDrugs.All(pairCovered.Contains)) {
                        var nearestToAorB = Enumerable.Range(0, branchCount)
                            .Where(c => c != a && c != b
                                && globalDist[a + 1][c + 1] >= 0
                                && globalDist[b + 1][c + 1] >= 0)
                            .OrderBy(c => Math.Min(globalDist[a + 1][c + 1], globalDist[b + 1][c + 1]))
                            .Take(KNearestNeighbors);

                        foreach (var c in nearestToAorB)
                            TryAddCluster(new[] { a + 1, b + 1, c + 1 }, "Baseline:ProximityTriple");
                    }
                }
            }
        }
        */

        if (validClusters.Count > 0)
        {
            string NameOf(int m) => m >= 1 && m <= evaluations.Count ? evaluations[m - 1].BranchName : $"?({m})";
            var dump = validClusters.Select((cluster, i) =>
            {
                var key = string.Join(",", cluster.OrderBy(x => x));
                var src = clusterSources.TryGetValue(key, out var s) ? s : "unknown";
                var names = string.Join(" → ", cluster.Select(NameOf));
                return $"  #{i + 1} [{src}] branches=[{string.Join(",", cluster)}] ({names})";
            });
            _logger.LogInformation(
                "OrderRoutingOrchestrator — {Count} cluster(s) SURVIVED filtering (pre-scoring):\n{Clusters}",
                validClusters.Count, string.Join("\n", dump));
        }
        else
        {
            _logger.LogWarning(
                "OrderRoutingOrchestrator — NO clusters survived filtering (in-range + coverage + dedup).");
        }

        if (validClusters.Count == 0)
            return null;

        var (bestPath, minDistance, winningCluster) = FindBestCluster(globalDist, validClusters);
        if (bestPath.Count == 0)
            return null;

        LogClusterComparison(globalDist, validClusters, winningCluster, evaluations);

        var orderedBranches = bestPath.Select(globalIdx => evaluations[globalIdx - 1]).ToList();
        return BuildLegsForBranches(orderedBranches, cartItems, evaluations, minDistance);
    }

    private static (List<int> BestPath, double MinDistance, List<int> WinningCluster) FindBestCluster(
        double[][] globalDist,
        List<List<int>> clusters)
    {
        double absoluteMinDistance = double.PositiveInfinity;
        var absoluteBestPath = new List<int>();
        var winningCluster = new List<int>();

        foreach (var cluster in clusters)
        {
            if (cluster is null || cluster.Count == 0)
                continue;

            int n = cluster.Count;

            var subDist = new double[n + 1][];
            for (int i = 0; i <= n; i++)
                subDist[i] = new double[n + 1];

            for (int i = 0; i <= n; i++)
            {
                int globalI = (i == 0) ? 0 : cluster[i - 1];
                for (int j = 0; j <= n; j++)
                {
                    int globalJ = (j == 0) ? 0 : cluster[j - 1];
                    subDist[i][j] = globalDist[globalI][globalJ];
                }
            }

            var result = SolveOpenTourHeldKarp(subDist);

            if (result.TotalKm < absoluteMinDistance)
            {
                absoluteMinDistance = result.TotalKm;
                winningCluster = cluster;
                absoluteBestPath = result.Order.Select(localIdx => cluster[localIdx]).ToList();
            }
        }

        return (absoluteBestPath, absoluteMinDistance, winningCluster);
    }

    private OrderRoutingPlan? BuildLegsForBranches(
        IReadOnlyList<BranchFulfillmentEvaluation> orderedBranches,
        IReadOnlyList<CartItemDto> cartItems,
        IReadOnlyList<BranchFulfillmentEvaluation> allEvaluations,
        double totalDistanceKm)
    {
        var quantityByDrug = cartItems.GroupBy(c => c.DrugId).ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));
        var assigned = new HashSet<Guid>();
        var legs = new List<OrderFulfillmentLegPlan>();

        foreach (var eval in orderedBranches)
        {
            var lineItems = new List<FulfilledLineItem>();
            foreach (var a in eval.AvailableItems)
            {
                if (assigned.Contains(a.DrugId)) continue;
                if (!quantityByDrug.TryGetValue(a.DrugId, out var qty)) continue;
                lineItems.Add(new FulfilledLineItem(a.DrugId, a.DrugName, a.DrugNameAr, qty, a.UnitPrice));
                assigned.Add(a.DrugId);
            }

            if (lineItems.Count == 0)
                continue;

            legs.Add(new OrderFulfillmentLegPlan
            {
                PharmacyId = eval.PharmacyId,
                BranchId = eval.BranchId,
                BranchName = eval.BranchName,
                DistanceKm = eval.DistanceKm,
                Items = lineItems,
                LegSubtotal = lineItems.Sum(i => i.LineTotal)
            });
        }

        if (legs.Count == 0)
            return null;

        var strategy = legs.Count == 1 ? "SinglePharmacy" : "MultiBranchSplit";
        return new OrderRoutingPlan
        {
            Strategy = strategy,
            Legs = legs,
            UnfulfillableItems = BuildUnfulfillable(cartItems, assigned, allEvaluations),
            TotalDistanceKm = Math.Round(totalDistanceKm, 3),
            Reasoning = DefaultReasoning(strategy, legs.Count)
        };
    }

    private void LogClusterComparison(
        double[][] globalDist,
        List<List<int>> clusters,
        List<int> winningCluster,
        IReadOnlyList<BranchFulfillmentEvaluation> evaluations)
    {
        string NameOf(int globalIdx) =>
            globalIdx >= 1 && globalIdx <= evaluations.Count ? evaluations[globalIdx - 1].BranchName : $"?({globalIdx})";

        var scored = clusters
            .Select(cluster =>
            {
                var (_, km, _) = FindBestCluster(globalDist, new List<List<int>> { cluster });
                return (Cluster: cluster, Km: km);
            })
            .OrderBy(x => x.Km)
            .ToList();

        var lines = scored.Select((x, i) =>
        {
            var names = string.Join(" → ", x.Cluster.Select(NameOf));
            var mark = ReferenceEquals(x.Cluster, winningCluster) ? " ★ WINNER" : string.Empty;
            return $"  #{i + 1}: [{names}] realTripKm={x.Km:F2}{mark}";
        });

        _logger.LogInformation(
            "OrderRoutingOrchestrator — Held-Karp scored {Count} valid cluster(s) on the shared global matrix; " +
            "shortest covering cluster wins:\n{Clusters}",
            scored.Count, string.Join("\n", lines));
    }

    private RouterDecision? ParseRouterDecision(string raw)
    {
        var json = ExtractJsonObject(raw);
        if (json is null)
        {
            _logger.LogWarning(
                "OrderRoutingOrchestrator — AI FALLBACK CAUSE #2a: router output contained no JSON object " +
                "(no '{{' … '}}' found) — the model replied with prose only. Raw: {Raw}", raw);
            return null;
        }

        try
        {
            var decision = JsonSerializer.Deserialize<RouterDecision>(json, JsonOptions);
            if (decision is null)
            {
                _logger.LogWarning(
                    "OrderRoutingOrchestrator — AI FALLBACK CAUSE #2b: router JSON deserialized to null. " +
                    "Extracted JSON: {Json}", json);
            }
            return decision;
        }
        catch (JsonException ex)
        {
            var salvaged = SalvageTruncatedClusters(json);
            if (salvaged is { Clusters.Count: > 0 })
            {
                _logger.LogWarning(
                    "OrderRoutingOrchestrator — router JSON was truncated; salvaged {Count} complete cluster(s) " +
                    "from the partial reply and continued.", salvaged.Clusters.Count);
                return salvaged;
            }

            _logger.LogWarning(ex,
                "OrderRoutingOrchestrator — AI FALLBACK CAUSE #2c: failed to parse router JSON (malformed / " +
                "wrong shape) and nothing could be salvaged. Extracted JSON: {Json} | Raw: {Raw}", json, raw);
            return null;
        }
    }

    private static RouterDecision? SalvageTruncatedClusters(string json)
    {
        var clusters = new List<RouterCluster>();
        var stack = new Stack<int>();

        for (int i = 0; i < json.Length; i++)
        {
            var ch = json[i];
            if (ch == '{')
            {
                stack.Push(i);
            }
            else if (ch == '}')
            {
                if (stack.Count == 0)
                    continue;

                var objStart = stack.Pop();
                var candidate = json[objStart..(i + 1)];

                if (candidate.Contains("\"branches\"", StringComparison.OrdinalIgnoreCase) &&
                    !candidate.Contains("\"clusters\"", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var cluster = JsonSerializer.Deserialize<RouterCluster>(candidate, JsonOptions);
                        if (cluster?.Branches is { Count: > 0 })
                            clusters.Add(cluster);
                    }
                    catch (JsonException) { }
                }
            }
        }

        return clusters.Count > 0 ? new RouterDecision { Clusters = clusters } : null;
    }


    private static string? ExtractJsonObject(string text)

    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        return start >= 0 && end > start ? text[start..(end + 1)] : null;
    }

    private OrderRoutingPlan BuildDeterministicPlan(
        IReadOnlyList<BranchFulfillmentEvaluation> evaluations,
        IReadOnlyList<CartItemDto> cartItems)
    {
        var quantityByDrug = cartItems.GroupBy(c => c.DrugId).ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

        var singleCover = evaluations.FirstOrDefault(e => e.CoversEntireCart);

        if (singleCover is not null)
        {
            var items = singleCover.AvailableItems
                .Select(a => new FulfilledLineItem(a.DrugId, a.DrugName, a.DrugNameAr, quantityByDrug[a.DrugId], a.UnitPrice))
                .ToList();


            var leg = new OrderFulfillmentLegPlan
            {
                PharmacyId = singleCover.PharmacyId,
                BranchId = singleCover.BranchId,
                BranchName = singleCover.BranchName,
                DistanceKm = singleCover.DistanceKm,
                Items = items,
                LegSubtotal = items.Sum(i => i.LineTotal)
            };

            return new OrderRoutingPlan
            {
                Strategy = "SinglePharmacy",
                Legs = [leg],
                UnfulfillableItems = [],
                TotalDistanceKm = singleCover.DistanceKm,
                Reasoning = $"Branch '{singleCover.BranchName}' fulfills 100% of the cart from a single " +
                            $"location ({singleCover.DistanceKm:F2} km), avoiding any order split."
            };
        }

        var remaining = new HashSet<Guid>(quantityByDrug.Keys);
        var legsByBranch = new Dictionary<Guid, OrderFulfillmentLegPlan>();
        var legOrder = new List<Guid>();

        foreach (var drugId in remaining.ToList())
        {
            var host = evaluations.FirstOrDefault(e => e.AvailableItems.Any(a => a.DrugId == drugId));
            if (host is null)
                continue;

            var avail = host.AvailableItems.First(a => a.DrugId == drugId);
            var line = new FulfilledLineItem(drugId, avail.DrugName, avail.DrugNameAr, quantityByDrug[drugId], avail.UnitPrice);

            if (legsByBranch.TryGetValue(host.BranchId, out var existing))
            {
                var merged = existing.Items.Append(line).ToList();
                legsByBranch[host.BranchId] = existing with { Items = merged, LegSubtotal = merged.Sum(i => i.LineTotal) };
            }
            else
            {
                legsByBranch[host.BranchId] = new OrderFulfillmentLegPlan
                {
                    PharmacyId = host.PharmacyId,
                    BranchId = host.BranchId,
                    BranchName = host.BranchName,
                    DistanceKm = host.DistanceKm,
                    Items = [line],
                    LegSubtotal = line.LineTotal
                };
                legOrder.Add(host.BranchId);
            }

            remaining.Remove(drugId);
        }

        var legs = legOrder.Select(id => legsByBranch[id]).ToList();

        var assigned = quantityByDrug.Keys.Where(d => !remaining.Contains(d)).ToHashSet();
        var unfulfillable = BuildUnfulfillable(cartItems, assigned, evaluations);

        var strategy = legs.Count <= 1 ? "SinglePharmacy" : "MultiBranchSplit";
        return new OrderRoutingPlan
        {
            Strategy = strategy,
            Legs = legs,
            UnfulfillableItems = unfulfillable,
            TotalDistanceKm = Math.Round(legs.Sum(l => l.DistanceKm), 3),
            Reasoning = legs.Count == 0
                ? "No branch can supply the requested items."
                : $"AI router unavailable — assigned each item to its nearest stocking branch across " +
                  $"{legs.Count} branch(es) as a safety fallback."
        };
    }

    private static string FormatDistancesForPrompt(
        double[][] dist,
        IReadOnlyList<BranchFulfillmentEvaluation> evaluations)
    {
        int n = evaluations.Count;
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("Patient → branches (nearest first):");
        var patientDists = Enumerable.Range(0, n)
            .Select(i => (m: i + 1, km: dist[0][i + 1]))
            .Where(x => x.km >= 0 && x.km < double.MaxValue / 2)
            .OrderBy(x => x.km);
        foreach (var (m, km) in patientDists)
            sb.AppendLine($"  patient→m{m}: {Math.Round(km, 2)} km");

        var pairs = new List<(int A, int B, double Km)>();
        for (int a = 0; a < n; a++)
        for (int b = a + 1; b < n; b++)
        {
            var d = dist[a + 1][b + 1];
            if (d >= 0 && d < double.MaxValue / 2)
                pairs.Add((a + 1, b + 1, Math.Round(d, 2)));
        }

        if (pairs.Count > 0)
        {
            var sorted = pairs.OrderBy(p => p.Km).ToList();
            var medianKm = sorted[sorted.Count / 2].Km;
            var nearby = sorted.Where(p => p.Km <= medianKm).ToList();

            sb.AppendLine("Branch↔branch pairs (nearest first; pairs above median omitted as too far):");
            foreach (var (a, b, km) in nearby)
                sb.AppendLine($"  m{a}↔m{b}: {km} km");
        }

        return sb.ToString();
    }


    private static IReadOnlyList<MissingItem> BuildUnfulfillable(
        IReadOnlyList<CartItemDto> cartItems,
        HashSet<Guid> assignedDrugs,
        IReadOnlyList<BranchFulfillmentEvaluation> evaluations)
    {
        var bestAvailableByDrug = evaluations
            .SelectMany(e => e.AvailableItems.Concat(e.MissingItems.Select(m =>
                new AvailableItem(m.DrugId, m.DrugName, m.DrugNameAr, m.QuantityNeeded, m.QuantityAvailable, 0))))
            .GroupBy(a => a.DrugId)
            .ToDictionary(g => g.Key, g => g.Max(x => x.QuantityAvailable));

        var nameByDrug = evaluations
            .SelectMany(e => e.AvailableItems
                .Select(a => (a.DrugId, a.DrugName, a.DrugNameAr))
                .Concat(e.MissingItems.Select(m => (m.DrugId, m.DrugName, m.DrugNameAr))))
            .GroupBy(x => x.DrugId)
            .ToDictionary(g => g.Key, g => (g.First().DrugName, g.First().DrugNameAr));

        return cartItems
            .Where(c => !assignedDrugs.Contains(c.DrugId))
            .GroupBy(c => c.DrugId)
            .Select(g =>
            {
                var first = g.First();
                var have = bestAvailableByDrug.TryGetValue(g.Key, out var q) ? q : 0;
                var (nameEn, nameAr) = nameByDrug.TryGetValue(g.Key, out var n)
                    ? n
                    : (first.DrugName, string.Empty);
                return new MissingItem(g.Key, nameEn, nameAr, g.Sum(x => x.Quantity), have);
            })
            .ToList();

    }

    private static string DefaultReasoning(string strategy, int legCount) =>
        strategy == "SinglePharmacy"
            ? "A single pharmacy covers the cart, minimizing splits and travel."
            : $"No single branch covers the cart; split across {legCount} branches to maximize coverage " +
            "while keeping distance low.";

    private static OrderRoutingPlan EmptyPlan(string reason) => new()
    {
        Strategy = "SinglePharmacy",
        Legs = [],
        UnfulfillableItems = [],
        TotalDistanceKm = 0,
        Reasoning = reason
    };

    private OrderRoutingPlan NothingAvailablePlan(IReadOnlyList<CartItemDto> cartItems) => new()
    {
        Strategy = "SinglePharmacy",
        Legs = [],
        UnfulfillableItems = cartItems
            .GroupBy(c => c.DrugId)
            .Select(g => new MissingItem(g.Key, g.First().DrugName, g.First().DrugNameAr, g.Sum(x => x.Quantity), 0))
            .ToList(),

        TotalDistanceKm = 0,
        Reasoning = "عذراً، لا يوجد أي فرع قريب يتوفر به أي من الأدوية المطلوبة حالياً."
    };

    private static OrderRoutingPlan AppendUnstockedItems(
        OrderRoutingPlan plan,
        IReadOnlyList<CartItemDto> unstockedItems,
        IReadOnlyList<BranchFulfillmentEvaluation> evaluations)
    {
        if (unstockedItems.Count == 0)
            return plan;

        var nameByDrug = evaluations
            .SelectMany(e => e.MissingItems.Select(m => (m.DrugId, m.DrugName, m.DrugNameAr)))
            .GroupBy(x => x.DrugId)
            .ToDictionary(g => g.Key, g => (g.First().DrugName, g.First().DrugNameAr));

        var extra = unstockedItems
            .GroupBy(c => c.DrugId)
            .Select(g =>
            {
                var first = g.First();
                var (nameEn, nameAr) = nameByDrug.TryGetValue(g.Key, out var n)
                    ? n
                    : (first.DrugName, first.DrugNameAr);
                return new MissingItem(g.Key, nameEn, nameAr, g.Sum(x => x.Quantity), 0);
            });

        return plan with { UnfulfillableItems = plan.UnfulfillableItems.Concat(extra).ToList() };
    }

    private async Task<RouteSummary?> BuildRouteSummaryAsync(
        GeoLocation patientLocation,
        OrderRoutingPlan plan,
        IReadOnlyList<BranchFulfillmentEvaluation> evaluations,
        bool producedByAi,
        FulfillmentMode fulfillmentMode,
        CancellationToken cancellationToken)
    {
        if (plan.Legs.Count == 0)
            return null;

        var coordByBranch = evaluations
            .Where(e => e.Latitude.HasValue && e.Longitude.HasValue)
            .GroupBy(e => e.BranchId)
            .ToDictionary(g => g.Key, g => (Lat: g.First().Latitude!.Value, Lon: g.First().Longitude!.Value));

        var legs = plan.Legs.ToList();
        var haveAllCoords = legs.All(l => coordByBranch.ContainsKey(l.BranchId));

        var coords = new List<(double Lat, double Lon)> { (patientLocation.Latitude, patientLocation.Longitude) };
        if (haveAllCoords)
            coords.AddRange(legs.Select(l => coordByBranch[l.BranchId]));

        double[][]? dist = null;
        if (haveAllCoords)
        {
            var matrix = await _osrmRoutingService.GetDistanceMatrixAsync(coords, cancellationToken);
            if (matrix.IsSuccess && matrix.DistancesKm.Length == coords.Count)
                dist = matrix.DistancesKm;
        }

        IReadOnlyList<int> order;
        double totalKm;
        if (dist is not null && legs.Count > 1)
        {
            (order, totalKm) = SolveOpenTourHeldKarp(dist);
        }
        else
        {
            order = Enumerable.Range(0, legs.Count).ToList();
            totalKm = dist is not null
                ? dist[0][1]
                : Math.Round(legs.Sum(l => l.DistanceKm), 3);
        }

        var stops = new List<RouteStop>(order.Count);
        var prevCoordIndex = 0;
        for (int i = 0; i < order.Count; i++)
        {
            var legIdx = order[i];
            var leg = legs[legIdx];
            var hopKm = dist is not null
                ? Math.Round(dist[prevCoordIndex][legIdx + 1], 3)
                : Math.Round(leg.DistanceKm, 3);

            stops.Add(new RouteStop
            {
                Order = i + 1,
                BranchId = leg.BranchId,
                BranchName = leg.BranchName,
                DistanceFromPreviousKm = hopKm,
                ItemsToCollect = leg.Items.Select(it => it.DrugNameAr).ToList()
            });

            prevCoordIndex = legIdx + 1;
        }

        var optimizedBy = producedByAi ? "AI-MultiAgent" : "Held-Karp (TSP fallback)";

        var description = string.Empty;
        if (fulfillmentMode == FulfillmentMode.Pickup)
        {
            description = await GeneratePickupRouteDescriptionAsync(stops, Math.Round(totalKm, 3), cancellationToken)
                ?? BuildArabicRouteDescription(stops, Math.Round(totalKm, 3));
        }


        return new RouteSummary
        {
            Stops = stops,
            TotalDistanceKm = Math.Round(totalKm, 3),
            OptimizedBy = optimizedBy,
            Description = description
        };
    }

    private async Task<string?> GeneratePickupRouteDescriptionAsync(
        IReadOnlyList<RouteStop> stops,
        double totalKm,
        CancellationToken cancellationToken)
    {
        if (stops.Count == 0)
            return null;

        try
        {
            var descKernel = _kernelProvider.GetKernel(ModelRole.Chat).Clone();
            descKernel.Plugins.Clear();

            var placeholders = stops
                .Select((s, i) => (Stop: s, Tag: $"{{BRANCH_{i + 1}}}"))
                .ToList();

            var stopsJson = JsonSerializer.Serialize(
                placeholders.Select(p => new
                {
                    stop = p.Stop.Order,
                    placeholder = p.Tag,
                    distanceFromPreviousKm = p.Stop.DistanceFromPreviousKm
                }), JsonOptions);

            var prompt =
                $$"""
                You are a friendly assistant in a pharmacy app, speaking DIRECTLY TO THE PATIENT who chose
                to pick up their order themselves. Describe the shortest pickup route that our routing
                algorithm already computed, so the patient knows where to go and in what order.

                Ordered stops (JSON, already in the correct visiting order).
                Each stop has a "placeholder" token — you MUST copy that token verbatim into your output
                wherever you mention that stop's pharmacy. The backend will replace each token with the
                real pharmacy name after you respond.
                {{stopsJson}}
                Total trip distance: {{totalKm}} km.

                Write the message in ARABIC and follow these rules exactly:
                - Address the patient directly in the second person ("أنت"/"عليك"), in a warm, natural
                  tone. Do NOT sound like a salesperson or a delivery driver.
                - Describe the movements IN THE GIVEN ORDER: which pharmacy to go to first, then next, etc.
                  Never change the order.
                - For each pharmacy, write its placeholder token EXACTLY (e.g. {BRANCH_1}) followed by
                  its distanceFromPreviousKm in km. Do NOT invent or change placeholder tokens.
                - Keep it short and organized — only where to go and each pharmacy's distance. Do NOT add
                  extra details, and do NOT mention any medicines or quantities.
                - You may end with the total trip distance in one short clause.
                - Return ONLY the plain Arabic text — no Markdown, no quotes, no JSON.
                - RESTRICTIONS:
                1- Never use any language other than Arabic.
                2- Do NOT use Chinese language.
                3- Do NOT mention any medicines, prices, or quantities.
                4- Do NOT add marketing fluff, extra tips, or unnecessary instructions.
                """;

            var chat = descKernel.GetRequiredService<IChatCompletionService>();
            var history = new ChatHistory();
            history.AddUserMessage(prompt);

            var settings = new PromptExecutionSettings {
                ExtensionData = new Dictionary<string, object> {
                    ["temperature"] = 0.2,
                }
            };

            var response = await chat.GetChatMessageContentAsync(
                history, settings, kernel: descKernel, cancellationToken: cancellationToken);

            var text = response.Content?.Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogWarning("OrderRoutingOrchestrator — LLM returned empty pickup description; using local template.");
                return null;
            }

            foreach (var (stop, tag) in placeholders)
                text = text.Replace(tag, stop.BranchName, StringComparison.Ordinal);

            if (placeholders.Any(p => text.Contains(p.Tag, StringComparison.Ordinal))) {
                _logger.LogWarning(
                    "OrderRoutingOrchestrator — LLM description still contains unreplaced placeholders; using local template.");
                return null;
            }

            _logger.LogInformation("OrderRoutingOrchestrator - LLM returned a complete description (placeholders resolved): {Text}", text);

            return text;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OrderRoutingOrchestrator — LLM pickup description failed (quota/error); using local template.");
            return null;
        }
    }

    private static (IReadOnlyList<int> Order, double TotalKm) SolveOpenTourHeldKarp(double[][] dist)
    {
        int n = dist.Length - 1;
        int full = 1 << n;

        var dp = new double[full][];
        var parent = new int[full][];
        for (int m = 0; m < full; m++)
        {
            dp[m] = new double[n];
            parent[m] = new int[n];
            Array.Fill(dp[m], double.PositiveInfinity);
            Array.Fill(parent[m], -1);
        }

        for (int j = 0; j < n; j++)
            dp[1 << j][j] = dist[0][j + 1];

        for (int mask = 1; mask < full; mask++)
        {
            for (int j = 0; j < n; j++)
            {
                if ((mask & (1 << j)) == 0) continue;
                var cur = dp[mask][j];
                if (double.IsPositiveInfinity(cur)) continue;

                for (int k = 0; k < n; k++)
                {
                    if ((mask & (1 << k)) != 0) continue;
                    var next = mask | (1 << k);
                    var cand = cur + dist[j + 1][k + 1];
                    if (cand < dp[next][k])
                    {
                        dp[next][k] = cand;
                        parent[next][k] = j;
                    }
                }
            }
        }

        int fullMask = full - 1;
        double best = double.PositiveInfinity;
        int endBranch = 0;
        for (int j = 0; j < n; j++)
        {
            if (dp[fullMask][j] < best)
            {
                best = dp[fullMask][j];
                endBranch = j;
            }
        }

        var order = new List<int>(n);
        int curMask = fullMask, cur2 = endBranch;
        while (cur2 != -1)
        {
            order.Add(cur2);
            var prev = parent[curMask][cur2];
            curMask ^= (1 << cur2);
            cur2 = prev;
        }
        order.Reverse();

        return (order, Math.Round(best, 3));
    }

    private static string BuildArabicRouteDescription(IReadOnlyList<RouteStop> stops, double totalKm)
    {
        if (stops.Count == 0)
            return "لا توجد محطات.";

        static string Items(RouteStop s) =>
            s.ItemsToCollect.Count > 0 ? $" واستلم: {string.Join("، ", s.ItemsToCollect)}" : string.Empty;

        if (stops.Count == 1)
            return $"توجّه إلى {stops[0].BranchName} (على بُعد {stops[0].DistanceFromPreviousKm:F2} كم)" +
                   $"{Items(stops[0])}. إجمالي المسافة {totalKm:F2} كم.";

        var parts = stops.Select((s, i) =>
            i == 0
                ? $"توجّه أولًا إلى {s.BranchName} (على بُعد {s.DistanceFromPreviousKm:F2} كم){Items(s)}"
                : $"ثم إلى {s.BranchName} (على بُعد {s.DistanceFromPreviousKm:F2} كم){Items(s)}");

        return string.Join("، ", parts) + $". إجمالي مسافة الرحلة {totalKm:F2} كم.";
    }

    private sealed record RouterDecision
    {
        [JsonPropertyName("clusters")] public List<RouterCluster>? Clusters { get; init; }
    }

    private sealed record RouterCluster
    {
        [JsonPropertyName("reasoning")] public string? Reasoning { get; init; }

        [JsonPropertyName("branches")] public List<int>? Branches { get; init; }
    }
}

#pragma warning restore SKEXP0001, SKEXP0110
