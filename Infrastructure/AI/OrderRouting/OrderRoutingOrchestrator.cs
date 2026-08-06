using System.Text.Json;
using System.Text.Json.Serialization;
using Application.DTOs.OrderRouting;
using Application.Services.OrderRouting;
using Application.Services.OrderSplitting.Models;
using Infrastructure.AI;
using Infrastructure.AI.Abstractions;
using Infrastructure.AI.Agents;
using Infrastructure.AI.Models;
using Infrastructure.AI.Plugins;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;
using ChatMessageContent = Microsoft.SemanticKernel.ChatMessageContent;

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


        if (evaluations.Count == 0)
            return NothingAvailablePlan(cartItems);

        RouterDecision? decision = null;
        try
        {
            decision = await RunAgentDecisionAsync(patientLocation, cartItems, evaluations, fulfillmentMode, cancellationToken);

        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "OrderRoutingOrchestrator — Agent decision failed; falling back to deterministic heuristic.");
        }

        // DIAGNOSTIC: separate the two distinct AI-failure cases so the logs say WHICH one happened:
        //   • decision == null  → the agent never produced a parseable RouterDecision JSON
        //                         (no final router message, quota error, or unparseable output).
        //   • decision != null but plan == null → the JSON WAS parsed, but none of its legs mapped
        //                         to real branches/drugs (e.g. hallucinated branchId), so it yielded
        //                         zero usable legs.
        OrderRoutingPlan? plan;
        if (decision is null)
        {
            _logger.LogWarning(
                "OrderRoutingOrchestrator — AI FALLBACK CAUSE #1: no parseable RouterDecision returned " +
                "(router produced no final JSON message, hit quota, or output was unparseable). " +
                "Falling back to Held-Karp.");
            plan = null;
        }
        else
        {
            plan = BuildPlanFromDecision(decision, evaluations, cartItems);
            if (plan is null || plan.Legs.Count == 0)
            {
                _logger.LogWarning(
                    "OrderRoutingOrchestrator — AI FALLBACK CAUSE #3: RouterDecision parsed OK " +
                    "(strategy={Strategy}, {LegCount} raw leg(s)) but produced ZERO usable legs after " +
                    "validation — likely hallucinated branchId(s)/drugId(s) not present in the evaluations. " +
                    "Raw legs: {RawLegs}. Falling back to Held-Karp.",
                    decision.Strategy,
                    decision.Legs?.Count ?? 0,
                    JsonSerializer.Serialize(decision.Legs, JsonOptions));
            }
        }

        // Track how the plan was produced so the route summary can label its optimizer honestly:
        // when the AI is unavailable (quota / null / unparseable), the Held-Karp TSP fallback both
        // orders the stops AND owns the reported trip distance.
        var producedByAi = plan is not null && plan.Legs.Count > 0;

        if (!producedByAi)
        {
            _logger.LogInformation(
                "OrderRoutingOrchestrator — Agent returned no usable plan; running Held-Karp TSP fallback.");
            plan = BuildDeterministicPlan(evaluations, cartItems);
        }
        else
        {
            _logger.LogInformation(
                "OrderRoutingOrchestrator — AI plan accepted: {LegCount} leg(s), tripDistanceKm={Trip}.",
                plan!.Legs.Count, plan.TotalDistanceKm);
        }


        // Attach a driver-facing, ordered route summary ("go to A first, then B, ..."). For an
        // AI plan we keep the AI's chosen leg order; for the fallback we compute the exact optimal
        // visiting order with Held-Karp. Best-effort: a summary failure never fails the plan.
        try
        {
            var summary = await BuildRouteSummaryAsync(patientLocation, plan!, evaluations, producedByAi, cancellationToken);

            if (summary is not null)
            {
                plan = plan! with
                {
                    RouteSummary = summary,
                    // The fallback's Held-Karp trip distance is the authoritative optimized metric.
                    TotalDistanceKm = producedByAi ? plan.TotalDistanceKm : summary.TotalDistanceKm
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


    public async Task<SplittingResult?> OptimizeSplitAsync(
        SplittingContext context,
        CancellationToken cancellationToken = default)

    {
        _logger.LogInformation(
            "OrderRoutingOrchestrator — Agent split for Order {OrderId}: {ItemCount} pending item(s), {BranchCount} candidate branch(es)",
            context.OrderId, context.PendingItems.Count, context.CandidateBranches.Count);

        if (context.PendingItems.Count == 0 || context.CandidateBranches.Count == 0)
            return null; // nothing the engine can improve on — let the deterministic algorithm decide

        SplitDecision? decision;
        try
        {
            decision = await RunSplitDecisionAsync(context, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "OrderRoutingOrchestrator — Agent split failed for Order {OrderId}; caller should fall back to deterministic algorithm.",
                context.OrderId);
            return null;
        }

        if (decision is null || decision.Assignments is null || decision.Assignments.Count == 0)
        {
            _logger.LogWarning(
                "OrderRoutingOrchestrator — Agent produced no usable split for Order {OrderId}; caller should fall back.",
                context.OrderId);
            return null;
        }

        return MapToSplittingResult(context, decision);
    }

    /// <summary>
    /// Reconciles the LLM's chosen assignments against the authoritative <see cref="SplittingContext"/>.
    /// The model only ever *chooses* which candidate branch fulfils which pending item; this method
    /// re-validates every choice against real available stock and greedily decrements a working copy
    /// so the engine can never over-commit inventory or reference a hallucinated branch/item.
    /// Any pending item the model fails to place is returned as unassigned.
    /// </summary>
    private SplittingResult MapToSplittingResult(SplittingContext context, SplitDecision decision)
    {
        var branchById = context.CandidateBranches.ToDictionary(b => b.BranchId);
        // Mutable working copy of available stock so multi-item legs don't double-spend.
        var remainingStock = context.CandidateBranches.ToDictionary(
            b => b.BranchId,
            b => b.AvailableStock.ToDictionary(kv => kv.Key, kv => kv.Value));

        var assignmentByItem = decision.Assignments
            .Where(a => a.OrderItemId != Guid.Empty)
            .GroupBy(a => a.OrderItemId)
            .ToDictionary(g => g.Key, g => g.First());

        var assignments = new List<ItemAssignment>();
        var unassigned = new List<Guid>();

        foreach (var item in context.PendingItems)
        {
            if (!assignmentByItem.TryGetValue(item.OrderItemId, out var choice) ||
                !branchById.TryGetValue(choice.BranchId, out var branch) ||
                !remainingStock.TryGetValue(choice.BranchId, out var stockByDrug) ||
                !stockByDrug.TryGetValue(item.DrugId, out var available) ||
                available < item.QuantityNeeded)
            {
                unassigned.Add(item.OrderItemId);
                continue;
            }

            var remaining = available - item.QuantityNeeded;
            stockByDrug[item.DrugId] = remaining;

            var coverage = context.PendingItems.Count(p => p.DrugId == item.DrugId);
            assignments.Add(new ItemAssignment(
                item.OrderItemId,
                branch.BranchId,
                item.DrugId,
                item.QuantityNeeded,
                new AssignmentDecision("AI-MultiAgent", coverage, branch.DistanceKm, remaining)));
        }

        _logger.LogInformation(
            "OrderRoutingOrchestrator — Agent split for Order {OrderId} mapped: {Assigned} assigned, {Unassigned} unassigned across {Branches} branch(es)",
            context.OrderId, assignments.Count, unassigned.Count,
            assignments.Select(a => a.BranchId).Distinct().Count());

        return new SplittingResult(assignments, unassigned);
    }

    private async Task<SplitDecision?> RunSplitDecisionAsync(
        SplittingContext context,
        CancellationToken cancellationToken)
    {
        // Only the tool-free router agent is needed here: the OrderSplittingService already gathered
        // the candidate branches (geo + fulfillment filtered) and inventory snapshot, so the model
        // reasons purely over the supplied context instead of calling inventory tools.
        var routerKernel = _kernelProvider.GetKernel(ModelRole.Chat).Clone();
        routerKernel.Plugins.Clear();

        var routerAgent = new ChatCompletionAgent
        {
            Name = OrderRoutingAgentDefinitions.RouteOptimizationAgentName,
            Instructions = OrderRoutingAgentDefinitions.RouteOptimizationAgentInstructions,
            Kernel = routerKernel
        };

        var pendingJson = JsonSerializer.Serialize(
            context.PendingItems.Select(p => new { p.OrderItemId, p.DrugId, p.QuantityNeeded }), JsonOptions);
        var branchesJson = JsonSerializer.Serialize(
            context.CandidateBranches.Select(b => new
            {
                b.BranchId,
                b.BranchName,
                b.DistanceKm,
                AvailableStock = b.AvailableStock
            }), JsonOptions);

        var prompt =
            $$"""
            You are allocating a patient's pending order items across candidate pharmacy branches.
            Fulfillment mode: {{context.FulfillmentMode}}.

            Pending items (JSON): {{pendingJson}}

            Candidate branches with available stock per DrugId (JSON): {{branchesJson}}

            RULES:
            - Prefer fulfilling the WHOLE order from the FEWEST branches; break ties by shortest DistanceKm.
            - You may only assign an item to a branch whose AvailableStock for that DrugId is >= QuantityNeeded.
            - Never exceed a branch's available stock across all items you assign to it.
            - If no branch can supply an item, leave it out (it will be marked unavailable).

            Respond with ONLY this JSON (no prose, no code fences):
            {
              "assignments": [ { "orderItemId": "<guid>", "branchId": "<guid>" } ]
            }
            """;

        var chat = routerKernel.GetRequiredService<IChatCompletionService>();
        var history = new ChatHistory();
        history.AddUserMessage(prompt);

        var response = await chat.GetChatMessageContentAsync(
            history, kernel: routerKernel, cancellationToken: cancellationToken);

        var raw = response.Content;
        if (string.IsNullOrWhiteSpace(raw))
        {
            _logger.LogWarning("OrderRoutingOrchestrator — Router produced no output for Order {OrderId}.", context.OrderId);
            return null;
        }

        var json = ExtractJsonObject(raw);
        if (json is null)
            return null;

        try
        {
            return JsonSerializer.Deserialize<SplitDecision>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "OrderRoutingOrchestrator — Failed to parse split JSON for Order {OrderId}: {Raw}",
                context.OrderId, raw);
            return null;
        }
    }

    private async Task<RouterDecision?> RunAgentDecisionAsync(
        GeoLocation patientLocation,
        IReadOnlyList<CartItemDto> cartItems,
        IReadOnlyList<BranchFulfillmentEvaluation> evaluations,
        FulfillmentMode fulfillmentMode,
        CancellationToken cancellationToken)
    {
        var workerKernel = _kernelProvider.GetKernel(ModelRole.Chat).Clone();
        // Build the plugin from the application root scope factory (has AppDbContext), NOT from
        // workerKernel.Services — the Kernel's internal DI container does not register app services.
        // Carry the order's fulfillment mode so the agent's own evaluate_candidate_branches tool call
        // applies the SAME geographic filter (Delivery=ServiceRadiusKm, Pickup=20km cap) as the
        // authoritative pre-computed evaluations — otherwise a Pickup order's tool call would fall
        // back to Delivery filtering and surface a different candidate set.
        workerKernel.Plugins.AddFromObject(
            new PharmacyInventoryPlugin(
                _scopeFactory,
                _osrmRoutingService,
                _loggerFactory.CreateLogger<PharmacyInventoryPlugin>())
            {
                DefaultFulfillmentMode = fulfillmentMode
            },
            pluginName: "PharmacyInventory");

        workerKernel.Plugins.AddFromObject(
            new GeoDistancePlugin(_osrmRoutingService, _loggerFactory.CreateLogger<GeoDistancePlugin>()),
            pluginName: "GeoDistance");

        // The router now OWNS the routing decision: it calls the OSRM-backed trip-distance tool to
        // compare candidate branch-sets and pick the one with the smallest TOTAL patient trip. So it
        // needs the GeoDistance plugin and function-calling enabled (it must NOT touch inventory tools).
        var routerKernel = _kernelProvider.GetKernel(ModelRole.Chat).Clone();
        routerKernel.Plugins.Clear();
        routerKernel.Plugins.AddFromObject(
            new GeoDistancePlugin(_osrmRoutingService, _loggerFactory.CreateLogger<GeoDistancePlugin>()),
            pluginName: "GeoDistance");

        var inventoryAgent = new ChatCompletionAgent
        {
            Name = OrderRoutingAgentDefinitions.InventoryCheckAgentName,
            Instructions = OrderRoutingAgentDefinitions.InventoryCheckAgentInstructions,
            Kernel = workerKernel,
            Arguments = new KernelArguments(
                new PromptExecutionSettings
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                })
        };

        var routerAgent = new ChatCompletionAgent
        {
            Name = OrderRoutingAgentDefinitions.RouteOptimizationAgentName,
            Instructions = OrderRoutingAgentDefinitions.RouteOptimizationAgentInstructions,
            Kernel = routerKernel,
            Arguments = new KernelArguments(
                new PromptExecutionSettings
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                })
        };


        var chat = new AgentGroupChat(inventoryAgent, routerAgent)
        {
            ExecutionSettings = new AgentGroupChatSettings
            {
                SelectionStrategy = new SequentialSelectionStrategy(),
                TerminationStrategy = new RouterTerminationStrategy(routerAgent)
                {
                    MaximumIterations = 2
                }
            }
        };

        var cartJson = JsonSerializer.Serialize(cartItems, JsonOptions);
        var evaluationsJson = JsonSerializer.Serialize(evaluations, JsonOptions);

        var seed =
            $"""
            Patient location: latitude={patientLocation.Latitude}, longitude={patientLocation.Longitude}.
            Cart items (JSON): {cartJson}

            InventoryCheckAgent: call the evaluation tool for these items and this location, then output the raw branch-evaluation JSON array.
            For reference, the pre-computed branch evaluations (each includes the branch Latitude and Longitude) are:
            {evaluationsJson}

            RouteOptimizationAgent: your GOAL is to MINIMIZE the patient's TOTAL trip distance.
            1. Identify the candidate branch-sets that TOGETHER cover the whole cart (a single fully-covering
               branch is one candidate; a combination of branches is another).
            2. For EACH candidate branch-set, call the GeoDistance.calculate_trip_distance_km tool with the
               patient coordinates (latitude={patientLocation.Latitude}, longitude={patientLocation.Longitude})
               and the ordered list of that set's branch coordinates (from each evaluation's Latitude/Longitude),
               trying a sensible visiting order (e.g. nearest branch first). The tool returns the real OSRM
               total trip km (patient -> branch -> branch ...). A return value of -1 means infeasible; discard it.
            3. Choose the branch-set with the SMALLEST returned trip distance and output the final routing plan
               JSON per your instructions, including that winning value as "tripDistanceKm".
            """;


        chat.AddChatMessage(new ChatMessageContent(AuthorRole.User, seed));

        // DIAGNOSTIC: capture EVERY agent turn so the logs show exactly what the group chat produced —
        // whether the router ever spoke, how many turns each agent took, and the raw text of each.
        // This is what tells us apart "router never emitted a final JSON" from "router emitted text we
        // couldn't parse".
        string? lastRouterMessage = null;
        var turnCount = 0;
        var routerTurns = 0;
        await foreach (var message in chat.InvokeAsync(cancellationToken))
        {
            turnCount++;
            var author = message.AuthorName ?? "(unknown)";
            var content = message.Content ?? string.Empty;

            _logger.LogInformation(
                "OrderRoutingOrchestrator — Agent turn #{Turn} by {Author} ({Len} chars): {Content}",
                turnCount, author, content.Length,
                content.Length > 2000 ? content[..2000] + "…(truncated)" : content);

            if (string.Equals(author, OrderRoutingAgentDefinitions.RouteOptimizationAgentName,
                    StringComparison.Ordinal))
            {
                routerTurns++;
                lastRouterMessage = message.Content;
            }
        }

        _logger.LogInformation(
            "OrderRoutingOrchestrator — Group chat finished: {TotalTurns} total turn(s), {RouterTurns} router turn(s).",
            turnCount, routerTurns);

        if (string.IsNullOrWhiteSpace(lastRouterMessage))
        {
            _logger.LogWarning(
                "OrderRoutingOrchestrator — AI FALLBACK CAUSE #1a: the RouteOptimizationAgent never produced " +
                "a final text message (it likely exhausted MaximumIterations={MaxIter} on tool calls, e.g. " +
                "calculate_trip_distance_km, before emitting its JSON). RouterTurns={RouterTurns}, TotalTurns={TotalTurns}.",
                2, routerTurns, turnCount);
            return null;
        }

        // Log the exact raw router text BEFORE parsing so we can see whether it's valid JSON, wrapped
        // in markdown fences, or truncated.
        _logger.LogInformation(
            "OrderRoutingOrchestrator — Raw router final message ({Len} chars): {Raw}",
            lastRouterMessage.Length, lastRouterMessage);

        return ParseRouterDecision(lastRouterMessage);
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
            _logger.LogWarning(ex,
                "OrderRoutingOrchestrator — AI FALLBACK CAUSE #2c: failed to parse router JSON (malformed / " +
                "wrong shape). Extracted JSON: {Json} | Raw: {Raw}", json, raw);
            return null;
        }
    }


    private static string? ExtractJsonObject(string text)
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        return start >= 0 && end > start ? text[start..(end + 1)] : null;
    }

    private OrderRoutingPlan? BuildPlanFromDecision(
        RouterDecision decision,
        IReadOnlyList<BranchFulfillmentEvaluation> evaluations,
        IReadOnlyList<CartItemDto> cartItems)
    {
        if (decision.Legs is null || decision.Legs.Count == 0)
            return null;

        var evalByBranch = evaluations.ToDictionary(e => e.BranchId);
        var quantityByDrug = cartItems.GroupBy(c => c.DrugId).ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

        var legs = new List<OrderFulfillmentLegPlan>();
        var assignedDrugs = new HashSet<Guid>();

        foreach (var legChoice in decision.Legs)
        {
            if (!evalByBranch.TryGetValue(legChoice.BranchId, out var eval))
            {
                _logger.LogWarning(
                    "OrderRoutingOrchestrator — Dropping leg: branchId {BranchId} is NOT in the candidate " +
                    "evaluations (hallucinated / out-of-set). Valid branchIds: {ValidIds}.",
                    legChoice.BranchId, string.Join(", ", evalByBranch.Keys));
                continue; // ignore hallucinated branches
            }

            var lineItems = new List<FulfilledLineItem>();
            var availableByDrug = eval.AvailableItems.ToDictionary(a => a.DrugId);

            var chosenDrugIds = legChoice.Items?.Select(i => i.DrugId) ?? availableByDrug.Keys;
            foreach (var drugId in chosenDrugIds)
            {
                if (assignedDrugs.Contains(drugId)) continue; 
                if (!availableByDrug.TryGetValue(drugId, out var avail))
                {
                    _logger.LogWarning(
                        "OrderRoutingOrchestrator — Leg for branch {BranchId} references drugId {DrugId} that " +
                        "the branch does NOT have available (hallucinated / not in AvailableItems); skipping it.",
                        legChoice.BranchId, drugId);
                    continue;
                }
                if (!quantityByDrug.TryGetValue(drugId, out var qty)) continue;

                lineItems.Add(new FulfilledLineItem(drugId, avail.DrugName, avail.DrugNameAr, qty, avail.UnitPrice));

                assignedDrugs.Add(drugId);
            }

            if (lineItems.Count == 0)
            {
                _logger.LogWarning(
                    "OrderRoutingOrchestrator — Leg for branch {BranchId} yielded no usable line items " +
                    "(all its drugs were already assigned or not available here); dropping the leg.",
                    legChoice.BranchId);
                continue;
            }


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

        var unfulfillable = BuildUnfulfillable(cartItems, assignedDrugs, evaluations);
        var strategy = legs.Count == 1 ? "SinglePharmacy" : "MultiBranchSplit";
        var reasoning = string.IsNullOrWhiteSpace(decision.Reasoning)
            ? DefaultReasoning(strategy, legs.Count)
            : decision.Reasoning!;

        // The router OWNS the optimization metric: prefer the exact trip distance it computed via the
        // OSRM trip tool (patient → branch → branch ...). Only if the model omitted it do we fall back
        // to summing the independent patient→branch legs (a rough proxy, not a real trip).
        var totalDistanceKm = decision.TripDistanceKm is > 0
            ? Math.Round(decision.TripDistanceKm.Value, 3)
            : Math.Round(legs.Sum(l => l.DistanceKm), 3);

        return new OrderRoutingPlan
        {
            Strategy = strategy,
            Legs = legs,
            UnfulfillableItems = unfulfillable,
            TotalDistanceKm = totalDistanceKm,
            Reasoning = reasoning
        };
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
        var legs = new List<OrderFulfillmentLegPlan>();
        var usedBranches = new HashSet<Guid>();

        while (remaining.Count > 0)
        {
            var best = evaluations
                .Where(e => !usedBranches.Contains(e.BranchId))
                .Select(e => new
                {
                    Eval = e,
                    Covers = e.AvailableItems.Where(a => remaining.Contains(a.DrugId)).ToList()
                })
                .Where(x => x.Covers.Count > 0)
                .OrderByDescending(x => x.Covers.Count)
                .ThenBy(x => x.Eval.DistanceKm)
                .FirstOrDefault();

            if (best is null)
                break;

            var items = best.Covers
                .Select(a => new FulfilledLineItem(a.DrugId, a.DrugName, a.DrugNameAr, quantityByDrug[a.DrugId], a.UnitPrice))
                .ToList();


            legs.Add(new OrderFulfillmentLegPlan
            {
                PharmacyId = best.Eval.PharmacyId,
                BranchId = best.Eval.BranchId,
                BranchName = best.Eval.BranchName,
                DistanceKm = best.Eval.DistanceKm,
                Items = items,
                LegSubtotal = items.Sum(i => i.LineTotal)
            });

            usedBranches.Add(best.Eval.BranchId);
            foreach (var line in items)
                remaining.Remove(line.DrugId);
        }

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
                : DefaultReasoning(strategy, legs.Count)
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
            .Select(g => new MissingItem(g.Key, g.First().DrugName, string.Empty, g.Sum(x => x.Quantity), 0))
            .ToList(),

        TotalDistanceKm = 0,
        Reasoning = "No nearby branch stocks any of the requested items."
    };

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

        // Materialize ordered stops with per-hop distances. In parallel we collect each stop's
        // ARABIC drug names (falling back to the English name when a drug has no Arabic name) so the
        // LLM can phrase the description with Arabic medicine names, while the persisted
        // RouteStop.ItemsToCollect stays on the English brand names used elsewhere in the UI.
        var stops = new List<RouteStop>(order.Count);
        var arabicItemsPerStop = new List<IReadOnlyList<string>>(order.Count);
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

            arabicItemsPerStop.Add(leg.Items
                .Select(it => string.IsNullOrWhiteSpace(it.DrugNameAr) ? it.DrugName : it.DrugNameAr)
                .ToList());

            prevCoordIndex = legIdx + 1;
        }

        var optimizedBy = producedByAi ? "AI-MultiAgent" : "Held-Karp (TSP fallback)";

        // Prefer a natural, human-friendly ARABIC description written by the LLM (with Arabic drug
        // names). If the model is unavailable (quota / error / empty), fall back to a locally-built
        // Arabic template so the response always ships an Arabic description regardless of AI.
        var description = await GenerateArabicDescriptionAsync(stops, arabicItemsPerStop, Math.Round(totalKm, 3), cancellationToken)
            ?? BuildArabicRouteDescription(stops, Math.Round(totalKm, 3));


        return new RouteSummary
        {
            Stops = stops,
            TotalDistanceKm = Math.Round(totalKm, 3),
            OptimizedBy = optimizedBy,
            Description = description
        };
    }

    /// <summary>
    /// Asks the LLM to phrase the already-decided, ordered pickup route as a friendly ARABIC
    /// sentence for the driver. The ORDER and distances are fixed here (by the AI plan or Held-Karp);
    /// the model only turns them into nice Arabic prose — it never changes the route. Returns null on
    /// any failure (quota / empty / exception) so the caller uses the local Arabic template instead.
    /// </summary>
    private async Task<string?> GenerateArabicDescriptionAsync(
        IReadOnlyList<RouteStop> stops,
        IReadOnlyList<IReadOnlyList<string>> arabicItemsPerStop,
        double totalKm,
        CancellationToken cancellationToken)
    {
        if (stops.Count == 0)
            return null;

        try
        {
            var descKernel = _kernelProvider.GetKernel(ModelRole.Chat).Clone();
            descKernel.Plugins.Clear();

            // Feed the model the ARABIC drug names (arabicItemsPerStop is aligned by index with stops)
            // so it writes medicine names in Arabic, not the English brand names.
            var stopsJson = JsonSerializer.Serialize(
                stops.Select((s, i) => new
                {
                    s.Order,
                    branchName = s.BranchName,
                    distanceFromPreviousKm = s.DistanceFromPreviousKm,
                    items = i < arabicItemsPerStop.Count ? arabicItemsPerStop[i] : s.ItemsToCollect
                }), JsonOptions);

            var prompt =
                $$"""
                أنت مساعد لوجيستي. اكتب وصفًا قصيرًا وواضحًا باللغة العربية لمسار مندوب التوصيل،
                يشرح الترتيب الذي يجب أن يمشي به: أين يذهب أولًا، ثم إلى أين، وماذا يستلم من كل فرع.

                نقاط التوقف بالترتيب (JSON): {{stopsJson}}
                إجمالي مسافة الرحلة: {{totalKm}} كم.

                القواعد:
                - لا تُغيّر الترتيب المُعطى إطلاقًا.
                - اذكر اسم كل فرع، والمسافة من النقطة السابقة، والأصناف التي يستلمها.
                - أسماء الأدوية في حقل "items" مكتوبة بالعربية؛ استخدمها كما هي بالعربية ولا تترجمها إلى الإنجليزية.
                - اجعل الوصف جملة أو جملتين فقط، بأسلوب مباشر وودود.
                - أعِد النص العربي فقط بدون أي تنسيق Markdown أو أقواس أو JSON.
                """;


            var chat = descKernel.GetRequiredService<IChatCompletionService>();
            var history = new ChatHistory();
            history.AddUserMessage(prompt);

            var response = await chat.GetChatMessageContentAsync(
                history, kernel: descKernel, cancellationToken: cancellationToken);

            var text = response.Content?.Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogWarning("OrderRoutingOrchestrator — LLM returned empty Arabic description; using local template.");
                return null;
            }

            return text;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OrderRoutingOrchestrator — LLM Arabic description failed (quota/error); using local template.");
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


    private sealed record RouterDecision
    {
        [JsonPropertyName("strategy")] public string? Strategy { get; init; }

        [JsonPropertyName("reasoning")] public string? Reasoning { get; init; }
        [JsonPropertyName("legs")] public List<RouterLeg>? Legs { get; init; }
        [JsonPropertyName("unfulfillableDrugIds")] public List<Guid>? UnfulfillableDrugIds { get; init; }

        /// <summary>
        /// The TOTAL trip distance (km) the router computed for its chosen legs by calling the
        /// GeoDistance.calculate_trip_distance_km tool (patient → branch → branch ...). This is the
        /// AI-owned optimization metric; the backend only stores it, it does not recompute it.
        /// </summary>
        [JsonPropertyName("tripDistanceKm")] public double? TripDistanceKm { get; init; }
    }


    private sealed record RouterLeg
    {
        [JsonPropertyName("branchId")] public Guid BranchId { get; init; }
        [JsonPropertyName("items")] public List<RouterItem>? Items { get; init; }
    }

    private sealed record RouterItem
    {
        [JsonPropertyName("drugId")] public Guid DrugId { get; init; }
        [JsonPropertyName("quantity")] public int Quantity { get; init; }
    }

    // --- Cart-to-Order pipeline decision shape (OptimizeSplitAsync) ---
    private sealed record SplitDecision
    {
        [JsonPropertyName("assignments")] public List<SplitAssignment>? Assignments { get; init; }
    }

    private sealed record SplitAssignment
    {
        [JsonPropertyName("orderItemId")] public Guid OrderItemId { get; init; }
        [JsonPropertyName("branchId")] public Guid BranchId { get; init; }
    }

    private sealed class RouterTerminationStrategy(Agent routerAgent) : TerminationStrategy
    {
        protected override Task<bool> ShouldAgentTerminateAsync(
            Agent agent,
            IReadOnlyList<ChatMessageContent> history,
            CancellationToken cancellationToken)
            => Task.FromResult(ReferenceEquals(agent, routerAgent));
    }
}

#pragma warning restore SKEXP0001, SKEXP0110
