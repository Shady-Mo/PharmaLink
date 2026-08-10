using System.Diagnostics;
using Infrastructure.AI.Execution.Providers;
using Infrastructure.AI.Execution.Resilience;
using Infrastructure.AI.Execution.Routing;

namespace Infrastructure.AI.Execution;

public class SemanticKernelPromptExecutionService : IPromptExecutionService
{
    private readonly IAIProviderRegistry _providerRegistry;
    private readonly IProviderRouter _providerRouter;
    private readonly IAIResiliencePipelineProvider _resiliencePipelineProvider;
    private readonly IPromptRegistry _promptRegistry;
    private readonly ILogger<SemanticKernelPromptExecutionService> _logger;

    public SemanticKernelPromptExecutionService(
        IAIProviderRegistry providerRegistry,
        IProviderRouter providerRouter,
        IAIResiliencePipelineProvider resiliencePipelineProvider,
        IPromptRegistry promptRegistry,
        ILogger<SemanticKernelPromptExecutionService> logger)
    {
        _providerRegistry = providerRegistry;
        _providerRouter = providerRouter;
        _resiliencePipelineProvider = resiliencePipelineProvider;
        _promptRegistry = promptRegistry;
        _logger = logger;
    }

    public async Task<PromptExecutionResult> ExecuteAsync(
        PromptExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        var prompt = await _promptRegistry.GetAsync(
            request.PromptName,
            request.PromptVersion,
            cancellationToken);

        var renderedPrompt = Render(prompt.Template, request.Variables);
        var targets = _providerRouter.GetTargets(request.TaskType, request.PromptName).ToList();

        if (targets.Count == 0)
        {
            throw new InvalidOperationException(
                $"No routing targets found for Task: {request.TaskType}, Prompt: {request.PromptName}");
        }

        var exceptions = new List<Exception>();

        foreach (var target in targets)
        {
            var circuitState = _resiliencePipelineProvider.GetCircuitState(target);
            if (circuitState == CircuitBreakerState.OpenCircuit || circuitState == CircuitBreakerState.Disabled)
            {
                _logger.LogWarning("Skipping target {Provider}:{Model} due to circuit state: {State}",
                    target.ProviderName, target.ModelId, circuitState);
                continue;
            }

            if (!_providerRegistry.TryGetProvider(target.ProviderName, out var executionProvider))
            {
                // Provider not registered — fall back to the generic SemanticKernel provider
                executionProvider = _providerRegistry.GetProvider("SemanticKernel");
            }

            var pipeline = _resiliencePipelineProvider.GetPipeline(target);
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var result = await pipeline.ExecuteAsync(async (ct) =>
                {
                    _logger.LogInformation("Executing Prompt={PromptName} Target={Provider}:{Model}", prompt.Name,
                        target.ProviderName, target.ModelId);

                    var execResult = await executionProvider.ExecuteAsync(target, request, prompt, renderedPrompt, ct);
                    if (!execResult.IsSuccess)
                    {
                        // Wrap business failures that are retryable into an exception so the Polly pipeline can handle it
                        throw new InvalidOperationException($"Provider execution failed: {execResult.Error}");
                    }

                    return execResult.Value!;
                }, cancellationToken);

                result.LatencyMs = stopwatch.ElapsedMilliseconds;

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("\n=========================================================");
                Console.WriteLine($"🤖 [AI MODEL REQUEST SUCCESS]");
                Console.WriteLine($"🔹 Provider : {target.ProviderName}");
                Console.WriteLine($"🔹 Model    : {target.ModelId}");
                Console.WriteLine($"🔹 Prompt   : {prompt.Name}");
                Console.WriteLine($"🔹 Latency  : {stopwatch.ElapsedMilliseconds} ms");
                Console.WriteLine("---------------------------------------------------------");
                Console.WriteLine("📄 [REQUEST RESULT]:");
                Console.WriteLine(result.RawResponse);
                Console.WriteLine("=========================================================\n");
                Console.ResetColor();

                return result;
            }
            catch (Exception ex) when (AIFallbackPolicy.IsTransient(ex))
            {
                stopwatch.Stop();
                _logger.LogWarning(ex,
                    "Transient failure for {Provider}:{Model} after {ElapsedMs}ms. Moving to next fallback target.",
                    target.ProviderName, target.ModelId, stopwatch.ElapsedMilliseconds);
                exceptions.Add(ex);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Fatal failure for {Provider}:{Model} after {ElapsedMs}ms. Moving to next fallback target.",
                    target.ProviderName, target.ModelId, stopwatch.ElapsedMilliseconds);
                exceptions.Add(ex);
                continue; // Treat non-transient errors as fallbackable too
            }
        }

        var fallbackChain = string.Join(" -> ", targets.Select(t => $"{t.ProviderName}:{t.ModelId}"));
        var errorDetails = string.Join("\n", exceptions.Select((e, i) => $"[Attempt {i + 1}] {e.GetType().Name}: {e.Message}"));
        var errorMsg =
            $"All AI providers failed for task {request.TaskType}. Fallback chain exhausted: {fallbackChain}\nFailure Details:\n{errorDetails}";
        
        _logger.LogError(new AggregateException(exceptions), "{ErrorMsg}", errorMsg);

        throw new InvalidOperationException(errorMsg, new AggregateException(exceptions));
    }

    private static string Render(string template, Dictionary<string, object?> variables)
    {
        var result = template;
        foreach (var kvp in variables)
        {
            var value = kvp.Value?.ToString() ?? string.Empty;
            result = result.Replace($"{{{{${kvp.Key}}}}}", value);
        }

        return result;
    }

    public async IAsyncEnumerable<string> ExecuteStreamAsync(
        PromptExecutionRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var prompt = await _promptRegistry.GetAsync(
            request.PromptName,
            request.PromptVersion,
            cancellationToken);

        var renderedPrompt = Render(prompt.Template, request.Variables);
        var targets = _providerRouter.GetTargets(request.TaskType, request.PromptName).ToList();

        if (targets.Count == 0)
        {
            throw new InvalidOperationException(
                $"No routing targets found for Task: {request.TaskType}, Prompt: {request.PromptName}");
        }

        var exceptions = new List<Exception>();

        foreach (var target in targets)
        {
            var circuitState = _resiliencePipelineProvider.GetCircuitState(target);
            if (circuitState == CircuitBreakerState.OpenCircuit || circuitState == CircuitBreakerState.Disabled)
            {
                _logger.LogWarning("Skipping target {Provider}:{Model} due to circuit state: {State}",
                    target.ProviderName, target.ModelId, circuitState);
                continue;
            }

            if (!_providerRegistry.TryGetProvider(target.ProviderName, out var executionProvider) || 
                (target.ProviderName == "Gemini" && request.TaskType != AITaskType.Vision))
            {
                // Fallback to the generic SK provider if specific one isn't found, or if it is Gemini for Chat/Agent tasks
                executionProvider = _providerRegistry.GetProvider("SemanticKernel");
            }

            var pipeline = _resiliencePipelineProvider.GetPipeline(target);
            var stopwatch = Stopwatch.StartNew();

            IAsyncEnumerator<string>? enumerator = null;
            bool streamStarted = false;
            bool hasFirstItem = false;
            long firstTokenLatencyMs = 0;

            try
            {
                // Use Polly pipeline for retrying the INITIAL connection (before any tokens are streamed)
                var result = await pipeline.ExecuteAsync(async (ct) =>
                {
                    _logger.LogInformation("Starting Stream Prompt={PromptName} Target={Provider}:{Model}", prompt.Name,
                        target.ProviderName, target.ModelId);

                    var stream = executionProvider.ExecuteStreamAsync(target, request, prompt, renderedPrompt, ct);
                    var enumr = stream.GetAsyncEnumerator(ct);

                    bool hasItem = await enumr.MoveNextAsync();

                    return (Enumerator: enumr, HasFirstItem: hasItem);
                }, cancellationToken);

                enumerator = result.Enumerator;
                hasFirstItem = result.HasFirstItem;
                firstTokenLatencyMs = stopwatch.ElapsedMilliseconds;
            }
            catch (Exception ex) when (AIFallbackPolicy.IsTransient(ex))
            {
                stopwatch.Stop();
                _logger.LogWarning(ex,
                    "Transient failure starting stream for {Provider}:{Model} after {ElapsedMs}ms. Moving to next fallback target.",
                    target.ProviderName, target.ModelId, stopwatch.ElapsedMilliseconds);
                exceptions.Add(ex);

                if (enumerator != null) await enumerator.DisposeAsync();
                continue; // Move to the next provider!
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Fatal non-transient failure starting stream for {Provider}:{Model}. Moving to next fallback target.", target.ProviderName, target.ModelId);
                exceptions.Add(ex);

                if (enumerator != null) await enumerator.DisposeAsync();
                continue; // Treat non-transient errors as fallbackable too
            }

            if (hasFirstItem)
            {
                streamStarted = true;
                yield return enumerator.Current;
            }

            // If the stream started but yielded no items, or started and yielded the first item...
            if (!streamStarted)
            {
                if (enumerator != null) await enumerator.DisposeAsync();
                yield break;
            }

            // Yield the rest of the stream WITHOUT Polly (Polly can't easily retry mid-stream)
            bool midStreamFailure = false;
            string? midStreamError = null;

            while (true)
            {
                try
                {
                    if (!await enumerator!.MoveNextAsync())
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Mid-stream failure for {Provider}:{Model}", target.ProviderName, target.ModelId);
                    midStreamFailure = true;
                    midStreamError = "\n\n[عذراً، حدث انقطاع في الاتصال أثناء المعالجة. يرجى المحاولة مرة أخرى.]";
                    break;
                }

                yield return enumerator!.Current;
            }

            await enumerator!.DisposeAsync();

            if (midStreamError != null)
            {
                yield return midStreamError;
            }

            stopwatch.Stop();
            _logger.LogInformation(
                "Successfully streamed Prompt={PromptName} via {Provider}:{Model} in {ElapsedMs}ms (FirstToken={FirstTokenMs}ms, MidStreamFailure={Failed})",
                prompt.Name, target.ProviderName, target.ModelId, stopwatch.ElapsedMilliseconds, firstTokenLatencyMs, midStreamFailure);

            // Do not fallback if we already streamed tokens, to avoid duplicate text.
            yield break;
        }

        var fallbackChain = string.Join(" -> ", targets.Select(t => $"{t.ProviderName}:{t.ModelId}"));
        var errorDetails = string.Join("\n", exceptions.Select((e, i) => $"[Attempt {i + 1}] {e.GetType().Name}: {e.Message}"));
        var errorMsg = $"All streaming targets failed for Prompt: {request.PromptName}. Fallback chain exhausted: {fallbackChain}\nFailure Details:\n{errorDetails}";

        _logger.LogError(new AggregateException(exceptions), "{ErrorMsg}", errorMsg);
        yield return "عذراً، خدمة الذكاء الاصطناعي تواجه ضغطاً شديداً أو غير متاحة حالياً بسبب استنفاد الحصة المجانية. يرجى المحاولة مرة أخرى لاحقاً.";
    }
}
