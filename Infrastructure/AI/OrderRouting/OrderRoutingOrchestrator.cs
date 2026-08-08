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

    // Per-request OUTPUT token budget. This is NOT the model's context limit — it's how many tokens the
    // model may GENERATE. The real 429 rate-limit problem came from multi-agent group chat + function-calling
    // which triggered ~5-10 requests in quick succession (each re-sending history+schemas), blowing the TPM
    // cap (Used 4515 + Requested 11118 > Limit 12000). RunAgentDecisionAsync now makes a SINGLE tool-free call,
    // but it asks for SEVERAL candidate clusters (each just a short list of branch indices). 1200 tokens
    // occasionally truncated the JSON on larger carts (several clusters + reasoning), which salvaged only a
    // partial reply and forced the Held-Karp fallback — so it was raised to 1600 for extra headroom. Setting
    // it too low truncates the JSON and breaks parsing. Do NOT set this to 2000+: that wastes the per-request
    // budget and risks hitting TPM again if multiple orders arrive together.
    public static readonly int MAX_TOKENS = 1200;

    // Held-Karp is O(n²·2ⁿ), so we cap how many branches ONE cluster may contain. A covering cluster never
    // needs more branches than there are distinct cart drugs, and we prefer few branches anyway, so 6 keeps
    // the exact TSP instant. Clusters larger than this (hallucinated or over-eager) are skipped.
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
        // Application root scope factory — the ONLY provider that has AppDbContext registered.
        // The Kernel's internal service provider (kernel.Services) does NOT contain application
        // services, so plugins must be built from this factory, never from kernel.Services.
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

        // Diagnostic: how many candidate branches the inventory plugin found for THIS call. This is the
        // FIRST gate before the AI clustering (and therefore before the "clusters SURVIVED filtering" log).
        // If this is 0, the method returns NothingAvailablePlan below and NONE of the cluster logs run —
        // which is the usual reason the logs appear for the order-routing PREVIEW (body location + default
        // Delivery mode) but NOT for create-order (saved DeliveryAddress location + the order's own
        // FulfillmentMode): a different location/mode can leave zero in-range branches.
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

        // ── PRE-FILTER (ROOT-CAUSE FIX for the hallucination): separate cart drugs that NO in-range branch
        //    stocks (e.g. a drug that exists in the catalog but is out of stock everywhere, or was just
        //    added and is nowhere nearby). Feeding these to the router is exactly what triggers the bad
        //    behaviour: the prompt says coverage is MANDATORY ("every cluster must stock EVERY cart drug"),
        //    so when a drug is stocked NOWHERE the model is cornered into either (a) FABRICATING a branch
        //    that "has" it — a hallucination — or (b) rambling/retrying until it BURNS the whole output-token
        //    budget and the JSON gets truncated. We therefore route ONLY the drugs at least one branch
        //    actually stocks, and fold the rest back in as unfulfillable at the very end. This makes the
        //    coverage rule satisfiable, shrinks the prompt, and removes the token-exhaustion path entirely.
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

        // If NOTHING in the cart is stocked anywhere, there is nothing to route at all.
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

        // NEW DIVISION OF LABOUR:
        //   • The AI PARTITIONS the shortlisted branches into candidate clusters — each cluster's branches
        //     TOGETHER cover the whole cart, and it groups branches that sit close to one another.
        //   • Held-Karp then measures every cluster's REAL open-tour trip on the SHARED distance matrix we
        //     already fetched (no extra OSRM calls) and keeps the shortest covering one
        //     (SelectBestCluster → FindBestCluster). So "minimum total distance" is decided
        //     deterministically here — a weak AI grouping can never inflate the final route.
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

        // Track how the plan was produced so the route summary can label its optimizer honestly:
        // when the AI is unavailable (quota / null / unparseable), the Held-Karp TSP fallback both
        // orders the stops AND owns the reported trip distance.
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

        // Fold the pre-filtered, stocked-nowhere drugs back into the plan as unfulfillable items so the
        // patient still sees them in the confirmation (they were simply never sent to the AI/clustering).
        plan = AppendUnstockedItems(plan!, unstockedItems, evaluations);



        // Attach a driver-facing, ordered route summary ("go to A first, then B, ..."). Division of
        // labour: the AI CHOSE the cluster (which branches + which drugs); Held-Karp then fixes the
        // exact optimal VISITING ORDER of those branches for BOTH the AI and fallback plans (reordering
        // never changes the chosen branches/drugs, only the driving sequence). Best-effort: a summary
        // failure never fails the plan.

        try
        {
            var summary = await BuildRouteSummaryAsync(patientLocation, plan!, evaluations, producedByAi, fulfillmentMode, cancellationToken);

            if (summary is not null)
            {
                plan = plan! with
                {
                    RouteSummary = summary,
                    // Held-Karp's exact OSRM trip distance is now the authoritative metric for BOTH paths:
                    // the AI only CHOOSES the branches (it no longer computes trip distance via a tool), so
                    // the real patient→branch→branch trip is always measured here deterministically.
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

        // SINGLE tool-free LLM call — the AI is the PRIMARY router and OWNS the branch-selection decision.
        // We deliberately do NOT use an AgentGroupChat + function-calling anymore: that fired ~5-10 chat
        // completions per order (each re-sending the whole history + tool schemas), which blew Groq's
        // per-minute token budget (HTTP 429). Instead we hand the model EVERYTHING it needs to decide in
        // ONE prompt:
        //   1. the candidate branches — already shortlisted upstream to the <=20 nearest branches that
        //      cover cart drugs (a cheap deterministic filter, NOT greedy set-cover), and
        //   2. a FULL driving-distance matrix (patient <-> every branch AND branch <-> branch), computed
        //      with ONE OSRM /table request, so the model can CLUSTER the branches that together cover the
        //      whole cart with the SMALLEST real total trip distance (patient -> branch -> branch ...),
        //      accounting for branch-to-branch legs — not just the nearest single patient hop.
        // The AI's candidate clusters are then each measured by Held-Karp (SelectBestClusterAsync) which
        // keeps the shortest fully-covering one and fixes its exact visiting order + authoritative trip
        // distance. If this call fails/returns nothing, the caller uses the minimal deterministic safety
        // fallback (greedy is disabled).

        var kernel = _kernelProvider.GetKernel(ModelRole.Chat).Clone();
        kernel.Plugins.Clear();

        // Coordinates for ONE OSRM /table request: index 0 = patient, index i+1 = evaluations[i]'s branch.
        // Every shortlisted branch is guaranteed to have coordinates (branches without them were filtered
        // out upstream because their distance couldn't be proven in-range), so the matrix rows line up 1:1
        // with `evaluations`.
        var coords = new List<(double Lat, double Lon)> { (patientLocation.Latitude, patientLocation.Longitude) };
        coords.AddRange(evaluations.Select(e => (e.Latitude ?? 0d, e.Longitude ?? 0d)));

        var matrix = await _osrmRoutingService.GetDistanceMatrixAsync(coords, cancellationToken);
        var haveMatrix = matrix.IsSuccess && matrix.DistancesKm.Length == coords.Count;

        // Branch list the model sees. `m` is the branch's index in the distance matrix (1..n) so the model
        // can look up patient->branch as M[0][m] and branch->branch as M[a][b]. Names/prices/Arabic are
        // resolved AFTERWARDS from the authoritative `evaluations` object (never from the model output), so
        // the prompt stays lean.
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

        // Compact the matrix for the prompt: round to 2 dp and map unreachable (OSRM MaxValue) hops to -1
        // so the model treats them as "never use" instead of parsing an astronomically large number. If
        // OSRM failed entirely, degrade gracefully to patient->branch distances only.
        string matrixJson;
        if (haveMatrix)
        {
            var cleaned = matrix.DistancesKm
                .Select(row => row.Select(v => v >= double.MaxValue / 2 ? -1d : Math.Round(v, 2)).ToArray())
                .ToArray();
            matrixJson = JsonSerializer.Serialize(cleaned, JsonOptions);
        }
        else
        {
            _logger.LogWarning(
                "OrderRoutingOrchestrator — OSRM distance matrix unavailable ({Msg}); the router will optimize " +
                "on patient->branch distances only (no branch<->branch data).", matrix.Message);
            matrixJson = JsonSerializer.Serialize(
                evaluations.Select(e => Math.Round(e.DistanceKm, 2)).ToArray(), JsonOptions);
        }

        var prompt =
            $$"""
            You are a pharmacy order-routing optimizer. PROPOSE several candidate CLUSTERS of branches that
            can each fulfil the WHOLE cart with the SHORTEST possible pickup trip. USE the distance matrix
            below to ESTIMATE and COMPARE each cluster's trip yourself, then propose the most promising ones.
            The backend re-verifies every cluster's exact trip with an optimal TSP solver and picks the winner.



            Cart items (JSON — each drugId with the quantity needed): {{cartJson}}


            Candidate branches (JSON). "m" is the branch's index in the distance matrix below;
            "coversEntireCart"=true means that single branch stocks every cart drug; "available" lists the
            DrugIds it can supply with the available quantity (qty):
            {{branchesJson}}

            Distance matrix (kilometres). Index 0 = the patient; index i = the branch whose "m" equals i.
            M[a][b] = driving distance from point a to point b. A value of -1 means that pair is unreachable —
            never use such a hop.
            {{(haveMatrix ? "M = " : "Patient->branch distances only (branch<->branch unavailable) = ")}}{{matrixJson}}

            HOW TO BUILD GOOD CLUSTERS:
            - RULE 1 — COVERAGE (mandatory): the branches in a cluster must TOGETHER stock EVERY cart drug.
              Check this against each branch's "available" list. A cluster that misses any cart drug is invalid.
            - RULE 2 — SHORT TRIP (use the matrix!): estimate a cluster's trip as the walk
              patient -> nearest branch -> next-nearest branch -> ..., adding M[0][m] for the patient->first
              hop and M[a][b] for each branch->branch hop. PREFER clusters with the SMALLEST estimated trip,
              and put that number in "estimatedTripKm".
            - RULE 3 — NO POINTLESS DETOURS: NEVER add a FAR branch (large M[0][m]) to a cluster when nearer
              branches already cover the same drugs. If two branches sit close to each other (small M[a][b])
              AND close to the patient (small M[0][m]), grouping THEM together is almost always best — do not
              pair one of them with a distant branch instead.
            - RULE 4 — MINIMALITY (NO REDUNDANT BRANCHES): 
              * Every branch in a cluster MUST contribute AT LEAST ONE UNIQUE DRUG that is not already provided by the other branches in that same cluster.
              * Never add extra branches to a cluster if the cart is already 100% covered by a smaller subset of those branches.
              * EXAMPLE A: If the patient requests Drug A, Pharmacy 1 (stocks A) and Pharmacy 2 (stocks A) must be returned as SEPARATE clusters:
                              `[1]` and `[2]`. NEVER combine them into `[1, 2]`.
              * EXAMPLE B: If the patient requests Drug B and Drug D, and Pharmacy 1 stocks (A, B) while Pharmacy 2 stocks (A, D),
                              combine them into ONE cluster `[1, 2]` because BOTH are required to complete the cart.
            - Prefer FEWER branches per cluster. If a branch has coversEntireCart=true, a single-branch
              cluster with just it is valid and is often the shortest. Never add extra branches to a cluster
              if the cart is already 100% covered by a smaller subset of those branches.
            - Propose 4 to 6 clusters ORDERED BEST-FIRST (smallest estimatedTripKm first). Make them
              genuinely different so the backend has real choices. Each cluster may contain at most
              {{MAX_CLUSTER_BRANCHES}} branches. Use ONLY the "m" indices shown above; never invent indices.

            KEEP THE OUTPUT SHORT so it is NEVER cut off by the token limit:
            - "reasoning" must be AT MOST 8 words and must NOT list DrugIds or repeat the matrix numbers.
            - Put "branches" FIRST in every cluster object, then "estimatedTripKm", then "reasoning".

            Respond with ONLY this JSON (no prose, no markdown, no code fences). "branches" is the list of
            branch "m" indices that form the cluster; "estimatedTripKm" is YOUR computed trip estimate:
            {
              "clusters": [
                { "branches": [<m>, ...], "estimatedTripKm": <number>, "reasoning": "<=8 words, no DrugIds>" }
              ]
            }


            """;




        var chat = kernel.GetRequiredService<IChatCompletionService>();
        var history = new ChatHistory();
        history.AddUserMessage(prompt);

        // temperature = 0 → make the router as DETERMINISTIC as possible. This is a routing/optimization
        // task with one right answer, not a creative one: the same cart + same branches should always
        // yield the same clusters. The default temperature (~0.7-1.0) is what caused the same order to
        // sometimes return a tight 0.3 km cluster and sometimes a bad 13 km one on different runs. Pinning
        // it to 0 sharply cuts that run-to-run variance (the backend baselines below still cap the worst
        // case even if the model slips). top_p = 1 keeps the full—but now greedily-ranked—token choice.
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


        // Log the exact raw router text BEFORE parsing so we can see whether it's valid JSON, wrapped in
        // markdown fences, or truncated (e.g. by too small a MAX_TOKENS).
        _logger.LogInformation(
            "OrderRoutingOrchestrator — Raw router message ({Len} chars): {Raw}",
            raw.Length, raw.Length > 2000 ? raw[..2000] + "…(truncated)" : raw);

        // Hand back the RAW global matrix (index 0 = patient, index i = branch whose "m"=i) so the caller
        // can slice it per cluster in FindBestCluster with NO further OSRM calls.
        return (ParseRouterDecision(raw), haveMatrix ? matrix.DistancesKm : null);
    }


    /// <summary>
    /// Deterministically turns the AI's proposed clusters into the winning <see cref="OrderRoutingPlan"/>.
    /// (1) Validate each AI cluster — keep only in-range branch indices whose branches TOGETHER cover the
    /// whole cart. (2) ALWAYS add the nearest single branch that covers the cart (if any) as a guaranteed
    /// baseline, so a single-branch answer can never be worse than "just drive to the closest covering
    /// branch". (3) ALSO add a deterministic MULTI-branch baseline — the set of nearest branches that
    /// together cover the cart — so that even if the AI proposes only far/hallucinated clusters, the
    /// backend still measures a tight covering option and can never pick a route worse than it. (4) Ask
    /// <see cref="FindBestCluster"/> to pick the cluster with the minimum real open-tour distance over the
    /// SHARED global matrix. (5) Build the legs in that optimal visiting order.
    /// </summary>
    private OrderRoutingPlan? SelectBestCluster(
        RouterDecision decision,
        double[][]? globalDist,
        IReadOnlyList<BranchFulfillmentEvaluation> evaluations,
        IReadOnlyList<CartItemDto> cartItems)
    {
        // Without the global matrix we cannot measure branch↔branch legs → let the deterministic fallback route it.
        if (globalDist is null)
        {
            _logger.LogWarning(
                "OrderRoutingOrchestrator — Global distance matrix unavailable; cannot score AI clusters. Falling back.");
            return null;
        }

        var cartDrugs = cartItems.Select(c => c.DrugId).ToHashSet();
        var validClusters = new List<List<int>>();
        var seen = new HashSet<string>(); // dedupe identical branch-sets
        // Remembers where each surviving cluster came from (AI vs a deterministic baseline), keyed by the
        // same sorted-index string used for de-duplication, purely so the diagnostic log below can label it.
        var clusterSources = new Dictionary<string, string>();

        void TryAddCluster(IEnumerable<int> branchIndices, string source)
        {
            var idx = branchIndices
                .Where(m => m >= 1 && m <= evaluations.Count) // in-range 1-based global index
                .Distinct()
                .ToList();
            if (idx.Count == 0 || idx.Count > MAX_CLUSTER_BRANCHES)
                return;

            // COVERAGE: the branches must TOGETHER stock every cart drug.
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

        // (1) the AI's proposed clusters (each a set of branch "m" indices).
        foreach (var c in decision.Clusters!)
            if (c.Branches is { Count: > 0 })
                TryAddCluster(c.Branches, "AI");

        // (2) GUARANTEED baseline #1 (single-branch): nearest single branch that alone covers the cart
        //     (evaluations are pre-sorted nearest-first within the covering tier). Even if the AI forgets
        //     the closest covering branch, it is always in the comparison — so a single-branch answer
        //     stays optimal.
        var nearestSingleCover = evaluations
            .Select((e, i) => (Eval: e, M: i + 1))
            .FirstOrDefault(x => x.Eval.CoversEntireCart);
        if (nearestSingleCover.Eval is not null)
            TryAddCluster(new[] { nearestSingleCover.M }, "Baseline:NearestSingleCover");

        // (3) GUARANTEED baseline #2 (multi-branch): the set of NEAREST branches that TOGETHER cover the
        //     cart. For every cart drug we take the closest branch that stocks it (evaluations are sorted
        //     nearest-first, so the FIRST match is the closest) and union those branches. This yields a
        //     deterministic, tight covering cluster built purely from the closest available stock, so even
        //     when the AI proposes ONLY far / hallucinated clusters, Held-Karp still measures this one and
        //     can never return a route worse than "collect each item from its nearest stocking branch".
        //     THIS is the main guard against the AI's occasional bad grouping (e.g. 13 km instead of 0.3 km):
        //     the AI can only ever IMPROVE on this baseline, never degrade below it.
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
                    break; // first match = nearest branch stocking this drug
                }
            }
        }
        if (nearestPerDrug.Count > 0)
            TryAddCluster(nearestPerDrug, "Baseline:NearestPerDrug");

        // DIAGNOSTIC: dump every cluster that SURVIVED TryAddCluster's filter (in-range + full coverage +
        // de-duplicated), tagged by where it came from (AI proposal vs the two deterministic baselines),
        // BEFORE Held-Karp scores them — so it's clear exactly what the winner is being chosen from. Note
        // that identical branch-sets are collapsed by de-dup, so a baseline that matches an AI cluster
        // keeps whichever source was added FIRST (AI is added first).
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

        // (4) deterministic winner = smallest real open-tour distance sliced from the global matrix.
        var (bestPath, minDistance, winningCluster) = FindBestCluster(globalDist, validClusters);
        if (bestPath.Count == 0)
            return null;

        LogClusterComparison(globalDist, validClusters, winningCluster, evaluations);

        // (5) build the legs following the OPTIMAL visiting order Held-Karp returned.
        var orderedBranches = bestPath.Select(globalIdx => evaluations[globalIdx - 1]).ToList();
        return BuildLegsForBranches(orderedBranches, cartItems, evaluations, minDistance);
    }

    /// <summary>
    /// Given the SHARED global distance matrix (index 0 = patient, 1..N = every candidate branch) and a set
    /// of candidate CLUSTERS (each a list of GLOBAL branch indices), returns the cluster whose optimal
    /// open-tour (patient → branch → …) has the SMALLEST total distance, together with that optimal visiting
    /// order (as global indices) and the distance. Each cluster is solved EXACTLY with Held-Karp on a
    /// sub-matrix sliced from the global matrix, so no extra distance lookups or OSRM calls are needed.
    /// </summary>
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

            // Slice an (n+1)×(n+1) sub-matrix: index 0 = patient (global 0); indices 1..n = this cluster's
            // branches, mapped through their global indices.
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
                // result.Order holds 0-based indices INTO this cluster; map them back to global indices.
                absoluteBestPath = result.Order.Select(localIdx => cluster[localIdx]).ToList();
            }
        }

        return (absoluteBestPath, absoluteMinDistance, winningCluster);
    }

    /// <summary>
    /// Builds an <see cref="OrderRoutingPlan"/> from the winning cluster's branches ALREADY IN OPTIMAL
    /// visiting order. Each cart drug is assigned to the FIRST branch in that order which stocks it, so a
    /// branch chosen for several drugs becomes one multi-item stop. The cluster is guaranteed to cover the
    /// cart, so there should be no unfulfillable items.
    /// </summary>
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
                continue; // every drug this branch stocks was already taken by an earlier stop

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

    /// <summary>
    /// Logs every VALID candidate cluster with the exact Held-Karp trip distance measured on the shared
    /// global matrix (winner marked ★). Distances are computed by the backend — never self-reported by the
    /// model — so ★ WINNER is the cluster that genuinely minimizes driving distance.
    /// </summary>
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
            // The reply was cut off mid-JSON (hit max_tokens), so the outer object never closed. Because
            // clusters are emitted BEST-FIRST, the earliest (shortest-trip) clusters are already complete —
            // salvage every fully-formed cluster object instead of discarding the whole (otherwise correct)
            // answer, so a truncation can no longer force a fallback to a worse route.
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

    /// <summary>
    /// Recovers usable clusters from a router reply whose JSON was cut off by the output-token limit.
    /// Walks the text with a brace stack so every COMPLETE <c>{ … }</c> object is captured even when the
    /// outer wrapper never closes, keeps the ones that carry a "branches" array (the clusters, not the
    /// wrapper), and deserializes them individually — silently dropping the final truncated fragment.
    /// Since the model emits clusters best-first, the salvaged set still holds the shortest-trip
    /// candidates, so Held-Karp can pick the real winner.
    /// </summary>
    private static RouterDecision? SalvageTruncatedClusters(string json)
    {
        var clusters = new List<RouterCluster>();
        var stack = new Stack<int>(); // start indices of currently-open '{'

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

                // Cluster objects carry a "branches" array; the outer wrapper carries "clusters" — skip it.
                if (candidate.Contains("\"branches\"", StringComparison.OrdinalIgnoreCase) &&
                    !candidate.Contains("\"clusters\"", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var cluster = JsonSerializer.Deserialize<RouterCluster>(candidate, JsonOptions);
                        if (cluster?.Branches is { Count: > 0 })
                            clusters.Add(cluster);
                    }
                    catch (JsonException)
                    {
                        // ignore a single malformed object; keep salvaging the rest
                    }
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

    /// <summary>
    /// LAST-RESORT deterministic safety fallback. The AI agent (<see cref="RunAgentDecisionAsync"/>) is
    /// the PRIMARY router and owns branch selection + clustering on the real OSRM trip distance; this
    /// method only runs when the LLM is completely unavailable (quota / network / unparseable output) so

    /// an order can still be routed instead of failing outright.
    ///
    /// ⚠️ The former GREEDY SET-COVER optimizer is intentionally DISABLED (commented out below) as part of
    /// the AI-first design — we do NOT want a heuristic silently competing with or masking the AI's
    /// decision. What remains is the cheapest correct behaviour only:
    ///   (1) if a single branch covers the whole cart, use the NEAREST such branch; else
    ///   (2) assign each drug to the NEAREST branch that stocks it (a plain per-drug nearest assignment,
    ///       NOT coverage-maximizing greedy), then let Held-Karp order the resulting stops.
    /// </summary>
    private OrderRoutingPlan BuildDeterministicPlan(
        IReadOnlyList<BranchFulfillmentEvaluation> evaluations,
        IReadOnlyList<CartItemDto> cartItems)
    {
        var quantityByDrug = cartItems.GroupBy(c => c.DrugId).ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

        // (1) Nearest single branch that covers the ENTIRE cart, if any. Evaluations are pre-sorted by
        //     coverage desc then distance asc, so FirstOrDefault(CoversEntireCart) is already the nearest.
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

        // ────────────────────────────────────────────────────────────────────────────────────────
        // ⛔ DISABLED — GREEDY SET-COVER OPTIMIZER (kept for reference, intentionally NOT executed).
        // This is the heuristic we deliberately turned OFF so the AI agent is the sole optimizer for
        // multi-branch orders. It repeatedly picked the branch covering the MOST remaining drugs
        // (tie-break by distance). Re-enabling it would let a heuristic compete with / mask the AI's
        // clustering decision, which is exactly what we do NOT want in the AI-first design.
        //
        // var remaining = new HashSet<Guid>(quantityByDrug.Keys);
        // var legs = new List<OrderFulfillmentLegPlan>();
        // var usedBranches = new HashSet<Guid>();
        //
        // while (remaining.Count > 0)
        // {
        //     var best = evaluations
        //         .Where(e => !usedBranches.Contains(e.BranchId))
        //         .Select(e => new
        //         {
        //             Eval = e,
        //             Covers = e.AvailableItems.Where(a => remaining.Contains(a.DrugId)).ToList()
        //         })
        //         .Where(x => x.Covers.Count > 0)
        //         .OrderByDescending(x => x.Covers.Count)   // ← greedy: maximize coverage per branch
        //         .ThenBy(x => x.Eval.DistanceKm)
        //         .FirstOrDefault();
        //
        //     if (best is null)
        //         break;
        //
        //     var items = best.Covers
        //         .Select(a => new FulfilledLineItem(a.DrugId, a.DrugName, a.DrugNameAr, quantityByDrug[a.DrugId], a.UnitPrice))
        //         .ToList();
        //
        //     legs.Add(new OrderFulfillmentLegPlan
        //     {
        //         PharmacyId = best.Eval.PharmacyId,
        //         BranchId = best.Eval.BranchId,
        //         BranchName = best.Eval.BranchName,
        //         DistanceKm = best.Eval.DistanceKm,
        //         Items = items,
        //         LegSubtotal = items.Sum(i => i.LineTotal)
        //     });
        //
        //     usedBranches.Add(best.Eval.BranchId);
        //     foreach (var line in items)
        //         remaining.Remove(line.DrugId);
        // }
        // ────────────────────────────────────────────────────────────────────────────────────────

        // ✅ ACTIVE fallback (NON-greedy): assign each still-needed drug to the NEAREST branch that
        // stocks it. Evaluations are pre-sorted by distance (within equal coverage), so the first
        // branch we encounter that has the drug is the closest one. Branches naturally coalesce into
        // legs (a branch chosen for several drugs becomes a single multi-item stop). Held-Karp then
        // orders the resulting stops into the shortest real trip — so even this safety path yields a
        // distance-sane route without any coverage-maximizing greedy logic.
        var remaining = new HashSet<Guid>(quantityByDrug.Keys);
        var legsByBranch = new Dictionary<Guid, OrderFulfillmentLegPlan>();
        var legOrder = new List<Guid>(); // preserve nearest-first discovery order

        foreach (var drugId in remaining.ToList())
        {
            // evaluations are already sorted nearest-first (within coverage tier), so First() = closest.
            var host = evaluations.FirstOrDefault(e => e.AvailableItems.Any(a => a.DrugId == drugId));
            if (host is null)
                continue; // no branch stocks this drug → stays unfulfillable

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

        // Bilingual name lookup sourced from the branch evaluations (catalog names), so unfulfillable
        // items still show the Arabic name in the confirmation popup. Falls back to the cart's
        // English name if the drug never appeared in any evaluation.
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

    /// <summary>
    /// Folds the drugs that NO in-range branch stocks (pre-filtered out before the AI ran) back into the
    /// plan as <see cref="MissingItem"/>s, so the patient still sees them as unfulfillable even though they
    /// were never sent to the router. Bilingual names are sourced from the branches' MissingItems (a drug
    /// stocked nowhere still appears there as "missing" in every branch), falling back to the cart's own
    /// English name and an empty Arabic name.
    /// </summary>
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

    /// <summary>
    /// Builds the driver-facing, ordered pickup route ("go to A first, then B, ...").
    ///
    /// The set of branches to visit is already fixed by the plan's legs; the only open question is
    /// the VISITING ORDER that minimizes total driving distance (patient → branch → branch → ...).
    /// That is an open-tour TSP over (patient + branch legs). We solve it exactly with Held-Karp
    /// (bitmask DP, O(n²·2ⁿ)) which is tiny and optimal for the handful of legs an order ever has.
    ///
    /// Distances come from ONE OSRM /table request over [patient, leg1, leg2, ...]; if OSRM is
    /// unavailable we can't order reliably, so we return null (the plan still ships without a summary).
    /// </summary>
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

        // A single stop needs no ordering — emit a trivial one-stop summary.
        var coordByBranch = evaluations
            .Where(e => e.Latitude.HasValue && e.Longitude.HasValue)
            .GroupBy(e => e.BranchId)
            .ToDictionary(g => g.Key, g => (Lat: g.First().Latitude!.Value, Lon: g.First().Longitude!.Value));

        // We can only order stops we have coordinates for. If any leg lacks coordinates, fall back to
        // the plan's own leg order rather than guessing.
        var legs = plan.Legs.ToList();
        var haveAllCoords = legs.All(l => coordByBranch.ContainsKey(l.BranchId));

        // Build coordinate list: index 0 = patient, 1..n = legs (same order as `legs`).
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

        // Determine the visiting order (indices into `legs`).
        IReadOnlyList<int> order;
        double totalKm;
        if (dist is not null && legs.Count > 1)
        {
            (order, totalKm) = SolveOpenTourHeldKarp(dist);
        }
        else
        {
            // No matrix (single stop or OSRM down): keep plan order; sum patient→branch leg distances.
            order = Enumerable.Range(0, legs.Count).ToList();
            totalKm = dist is not null
                ? dist[0][1] // single stop: patient → the one branch
                : Math.Round(legs.Sum(l => l.DistanceKm), 3);
        }

        // Materialize ordered stops with per-hop distances. RouteStop.ItemsToCollect keeps the Arabic
        // drug names for the UI; the patient-facing pickup description no longer lists medicines, so we
        // don't build a separate per-stop item list for the LLM anymore.
        var stops = new List<RouteStop>(order.Count);
        var prevCoordIndex = 0; // patient
        for (int i = 0; i < order.Count; i++)
        {
            var legIdx = order[i];
            var leg = legs[legIdx];
            var hopKm = dist is not null
                ? Math.Round(dist[prevCoordIndex][legIdx + 1], 3) // +1: coords are patient-offset
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

        // The patient-facing pickup route description only makes sense when the PATIENT is the one
        // travelling to collect the order (Pickup). For Delivery a courier drives the route, so we skip
        // the "go here first, then there" narration entirely and leave the description empty.
        var description = string.Empty;
        if (fulfillmentMode == FulfillmentMode.Pickup)
        {
            // Prefer a natural, patient-facing description written by the LLM; if the model is
            // unavailable (quota / error / empty), fall back to the local Arabic template so a pickup
            // order always ships a description regardless of AI.
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

    /// <summary>
    /// Asks the LLM to phrase the already-decided, ordered PICKUP route as a short, friendly message
    /// addressed directly to the PATIENT ("you go to … first, then …"). The visiting ORDER and the
    /// distances are fixed upstream (AI plan or Held-Karp) — the model only turns them into nice prose
    /// and never changes the route. The prompt is in English for maintainability but asks for Arabic
    /// output (the patient-facing language). We deliberately do NOT cap max_tokens here so the sentence
    /// is never cut off mid-word; brevity is enforced by the instructions instead. Returns null on any
    /// failure (quota / empty / exception) so the caller uses the local Arabic template.
    /// </summary>
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

            // Only the data the description needs: the ordered stop number, branch name, and how far
            // that branch is from the previous point. No medicine names — the patient message stays lean.
            var stopsJson = JsonSerializer.Serialize(
                stops.Select(s => new
                {
                    s.Order,
                    branchName = s.BranchName,
                    distanceFromPreviousKm = s.DistanceFromPreviousKm
                }), JsonOptions);

            var prompt =
                $$"""
                You are a friendly assistant in a pharmacy app, speaking DIRECTLY TO THE PATIENT who chose
                to pick up their order themselves. Describe the shortest pickup route that our routing
                algorithm already computed, so the patient knows where to go and in what order.

                Ordered stops (JSON, already in the correct visiting order):
                {{stopsJson}}
                Total trip distance: {{totalKm}} km.

                Write the message in ARABIC and follow these rules exactly:
                - Address the patient directly in the second person ("أنت"/"عليك"), in a warm, natural
                  tone. Do NOT sound like a salesperson or a delivery driver.
                - Describe the movements IN THE GIVEN ORDER: which pharmacy to go to first, then next, etc.
                  Never change the order.
                - For each pharmacy, mention its name and how far it is (use distanceFromPreviousKm, in km).
                - Keep it short and organized — only where to go and each pharmacy's distance. Do NOT add
                  extra details, and do NOT mention any medicines or quantities.
                - You may end with the total trip distance in one short clause.
                - Return ONLY the plain Arabic text — no Markdown, no quotes, no JSON.
                - RESTRICTIONS:
                1- Do NOT use any language other than Arabic.
                2- Do NOT mention any medicines, prices, or quantities.
                3- Do NOT add marketing fluff, extra tips, or unnecessary instructions.
                - EXAMPLE OUTPUT STRUCTURE (for reference only):
                  لتجميع طلبيتك، يمكنك البدء بالتوجه إلى فرع [Pharmacy Name] على بعد [Distance] كم،
                  ثم التوجه إلى فرع [Pharmacy Name] على بعد [Distance] كم. إجمالي مسافة الرحلة هو [TotalKm] كم.
                """;

            var chat = descKernel.GetRequiredService<IChatCompletionService>();
            var history = new ChatHistory();
            history.AddUserMessage(prompt);

            // NOTE: no max_tokens on purpose — capping it previously truncated the sentence mid-word.
            // Brevity is enforced by the prompt instructions, not by the output token budget.
            var response = await chat.GetChatMessageContentAsync(
                history, kernel: descKernel, cancellationToken: cancellationToken);

            var text = response.Content?.Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogWarning("OrderRoutingOrchestrator — LLM returned empty pickup description; using local template.");
                return null;
            }

            _logger.LogInformation($"OrderRoutingOrchestrator - LLM returned a complete description; {text}");

            return text;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OrderRoutingOrchestrator — LLM pickup description failed (quota/error); using local template.");
            return null;
        }
    }


    /// <summary>
    /// Exact open-tour TSP via Held-Karp. Node 0 is the fixed start (patient); nodes 1..n are the
    /// branches to visit (open tour — no return to start). <paramref name="dist"/> is the full
    /// (n+1)×(n+1) distance matrix. Returns the optimal visiting order as 0-based BRANCH indices
    /// (i.e. matrix index minus 1) and the minimal total distance.
    /// </summary>
    private static (IReadOnlyList<int> Order, double TotalKm) SolveOpenTourHeldKarp(double[][] dist)
    {
        int n = dist.Length - 1; // number of branches (excluding patient at index 0)
        int full = 1 << n;

        // dp[mask][j] = min distance to start at patient, visit exactly the branches in `mask`,
        //               and end at branch j (j must be in mask). Branch j maps to matrix index j+1.
        var dp = new double[full][];
        var parent = new int[full][];
        for (int m = 0; m < full; m++)
        {
            dp[m] = new double[n];
            parent[m] = new int[n];
            Array.Fill(dp[m], double.PositiveInfinity);
            Array.Fill(parent[m], -1);
        }

        // Base case: patient → branch j as the first (and only-so-far) stop.
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
                    if ((mask & (1 << k)) != 0) continue; // k already visited
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

        // Open tour: pick the cheapest end branch over the full set (no return leg to patient).
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

        // Reconstruct the order by walking parents backwards.
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

    /// <summary>
    /// Locally-built ARABIC route description used when the LLM is unavailable (quota / error).
    /// Deterministic, no external calls — guarantees the response always carries an Arabic summary.
    /// </summary>
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


    /// <summary>
    /// The router LLM's output shape. The model no longer picks a single winner — it returns SEVERAL
    /// candidate <see cref="RouterCluster"/>s (each a fully-covering set of legs). The backend then
    /// measures every cluster's real driving trip with Held-Karp and keeps the shortest
    /// (see SelectBestClusterAsync), so the minimum-distance decision is owned deterministically by us,
    /// not by the model's own (unverified) distance estimate.
    /// </summary>
    private sealed record RouterDecision
    {
        [JsonPropertyName("clusters")] public List<RouterCluster>? Clusters { get; init; }
    }

    /// <summary>
    /// One candidate cluster the AI proposes: a small group of branch matrix indices ("m") that TOGETHER
    /// cover the whole cart. The model now ESTIMATES the trip distance from the matrix and orders clusters
    /// best-first. The backend (<see cref="FindBestCluster"/>) re-verifies with exact Held-Karp and picks
    /// the shortest.
    /// </summary>
    private sealed record RouterCluster
    {
        /// <summary>One short model-written sentence explaining why this cluster is a tight, covering group.</summary>
        [JsonPropertyName("reasoning")] public string? Reasoning { get; init; }

        /// <summary>The AI's estimated trip distance (patient → branches) in km, computed from the matrix.</summary>
        [JsonPropertyName("estimatedTripKm")] public double? EstimatedTripKm { get; init; }

        /// <summary>The branch "m" indices (into the distance matrix, 1-based) that make up this cluster.</summary>
        [JsonPropertyName("branches")] public List<int>? Branches { get; init; }
    }
}

#pragma warning restore SKEXP0001, SKEXP0110
