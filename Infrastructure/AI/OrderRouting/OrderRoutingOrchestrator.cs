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
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "OrderRoutingOrchestrator — Optimizing fulfillment for Patient {PatientId}, {ItemCount} cart items",
            patientUserId, cartItems.Count);

        if (cartItems.Count == 0)
            return EmptyPlan("Cart is empty; nothing to route.");

        var evaluations = await _inventoryPlugin.EvaluateAsync(patientLocation, cartItems, cancellationToken);

        if (evaluations.Count == 0)
            return NothingAvailablePlan(cartItems);

        RouterDecision? decision = null;
        try
        {
            decision = await RunAgentDecisionAsync(patientLocation, cartItems, evaluations, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "OrderRoutingOrchestrator — Agent decision failed; falling back to deterministic heuristic.");
        }

        var plan = decision is not null
            ? BuildPlanFromDecision(decision, evaluations, cartItems)
            : null;

        if (plan is null || plan.Legs.Count == 0)
        {
            _logger.LogInformation("OrderRoutingOrchestrator — Using deterministic greedy plan.");
            plan = BuildDeterministicPlan(evaluations, cartItems);
        }

        _logger.LogInformation(
            "OrderRoutingOrchestrator — Plan '{Strategy}': {LegCount} leg(s), {TotalKm:F2} km, fullyFulfilled={Full}",
            plan.Strategy, plan.FulfillmentLegCount, plan.TotalDistanceKm, plan.IsFullyFulfilled);

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
        CancellationToken cancellationToken)
    {
        var workerKernel = _kernelProvider.GetKernel(ModelRole.Chat).Clone();
        // Build the plugin from the application root scope factory (has AppDbContext), NOT from
        // workerKernel.Services — the Kernel's internal DI container does not register app services.
        workerKernel.Plugins.AddFromObject(
            new PharmacyInventoryPlugin(
                _scopeFactory,
                _osrmRoutingService,
                _loggerFactory.CreateLogger<PharmacyInventoryPlugin>()),
            pluginName: "PharmacyInventory");
        workerKernel.Plugins.AddFromObject(
            new GeoDistancePlugin(_osrmRoutingService, _loggerFactory.CreateLogger<GeoDistancePlugin>()),
            pluginName: "GeoDistance");

        var routerKernel = _kernelProvider.GetKernel(ModelRole.Chat).Clone();
        routerKernel.Plugins.Clear();

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
            Kernel = routerKernel
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
            For reference, the pre-computed branch evaluations are:
            {evaluationsJson}

            RouteOptimizationAgent: using the branch evaluations, output the final routing plan JSON per your instructions.
            """;

        chat.AddChatMessage(new ChatMessageContent(AuthorRole.User, seed));

        string? lastRouterMessage = null;
        await foreach (var message in chat.InvokeAsync(cancellationToken))
        {
            if (string.Equals(message.AuthorName, OrderRoutingAgentDefinitions.RouteOptimizationAgentName,
                    StringComparison.Ordinal))
            {
                lastRouterMessage = message.Content;
            }
        }

        if (string.IsNullOrWhiteSpace(lastRouterMessage))
        {
            _logger.LogWarning("OrderRoutingOrchestrator — Router agent produced no output.");
            return null;
        }

        return ParseRouterDecision(lastRouterMessage);
    }

    private RouterDecision? ParseRouterDecision(string raw)
    {
        var json = ExtractJsonObject(raw);
        if (json is null)
            return null;

        try
        {
            return JsonSerializer.Deserialize<RouterDecision>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "OrderRoutingOrchestrator — Failed to parse router JSON: {Raw}", raw);
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
                continue; // ignore hallucinated branches

            var lineItems = new List<FulfilledLineItem>();
            var availableByDrug = eval.AvailableItems.ToDictionary(a => a.DrugId);

            var chosenDrugIds = legChoice.Items?.Select(i => i.DrugId) ?? availableByDrug.Keys;
            foreach (var drugId in chosenDrugIds)
            {
                if (assignedDrugs.Contains(drugId)) continue; 
                if (!availableByDrug.TryGetValue(drugId, out var avail)) continue;
                if (!quantityByDrug.TryGetValue(drugId, out var qty)) continue;

                lineItems.Add(new FulfilledLineItem(drugId, avail.DrugName, qty, avail.UnitPrice));
                assignedDrugs.Add(drugId);
            }

            if (lineItems.Count == 0) continue;

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

        return new OrderRoutingPlan
        {
            Strategy = strategy,
            Legs = legs,
            UnfulfillableItems = unfulfillable,
            TotalDistanceKm = Math.Round(legs.Sum(l => l.DistanceKm), 3),
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
                .Select(a => new FulfilledLineItem(a.DrugId, a.DrugName, quantityByDrug[a.DrugId], a.UnitPrice))
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
                .Select(a => new FulfilledLineItem(a.DrugId, a.DrugName, quantityByDrug[a.DrugId], a.UnitPrice))
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
                new AvailableItem(m.DrugId, m.DrugName, m.QuantityNeeded, m.QuantityAvailable, 0))))
            .GroupBy(a => a.DrugId)
            .ToDictionary(g => g.Key, g => g.Max(x => x.QuantityAvailable));

        return cartItems
            .Where(c => !assignedDrugs.Contains(c.DrugId))
            .GroupBy(c => c.DrugId)
            .Select(g =>
            {
                var first = g.First();
                var have = bestAvailableByDrug.TryGetValue(g.Key, out var q) ? q : 0;
                return new MissingItem(g.Key, first.DrugName, g.Sum(x => x.Quantity), have);
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
            .Select(g => new MissingItem(g.Key, g.First().DrugName, g.Sum(x => x.Quantity), 0))
            .ToList(),
        TotalDistanceKm = 0,
        Reasoning = "No nearby branch stocks any of the requested items."
    };

    private sealed record RouterDecision
    {
        [JsonPropertyName("strategy")] public string? Strategy { get; init; }
        [JsonPropertyName("reasoning")] public string? Reasoning { get; init; }
        [JsonPropertyName("legs")] public List<RouterLeg>? Legs { get; init; }
        [JsonPropertyName("unfulfillableDrugIds")] public List<Guid>? UnfulfillableDrugIds { get; init; }
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
