using Application.Services.AI;
using Application.Settings;
using Infrastructure.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Moq;
// Alias to avoid collision with Infrastructure.Options namespace from global usings
using MSOptions = Microsoft.Extensions.Options.Options;

namespace Infrastructure.UnitTests.AI;

/// <summary>
/// Unit tests for PharmacyAssistantService.
///
/// DESIGN DECISION — Mocking IChatCompletionService correctly:
///   SK's GetChatMessageContentAsync() is an EXTENSION METHOD on IChatCompletionService.
///   Moq cannot mock extension methods — it throws NotSupportedException.
///
///   The actual interface method (non-extension) is GetChatMessageContentsAsync()
///   (plural "Contents"), which returns IReadOnlyList<ChatMessageContent>.
///   We mock THAT and let the extension method call through it naturally.
///
/// DESIGN DECISION — What we test vs. what we don't:
///   We test our code (ChatHistory construction, settings application, error handling).
///   We do NOT test SK internals or mock the HTTP layer.
/// </summary>
public class PharmacyAssistantServiceTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private static IOptions<SemanticKernelSettings> CreateSettings(
        int maxHistoryTurns = 10,
        string provider = "GoogleGemini",
        int maxTokens = 2048,
        double temperature = 0.7)
    {
        return MSOptions.Create(new SemanticKernelSettings
        {
            Provider = provider,
            ModelId = "gemini-3.5-flash",
            ApiKey = "test-key",
            MaxTokens = maxTokens,
            Temperature = temperature,
            MaxHistoryTurns = maxHistoryTurns
        });
    }

    /// <summary>
    /// Creates a PharmacyAssistantService with a mock IChatCompletionService that
    /// returns the specified response when GetChatMessageContentsAsync is called.
    ///
    /// IMPORTANT: We mock GetChatMessageContentsAsync (plural) — the actual interface
    /// method — NOT GetChatMessageContentAsync (singular) which is an extension method.
    /// </summary>
    private static (PharmacyAssistantService Service, Mock<IChatCompletionService> MockChat)
        CreateService(
            string chatResponse = "Test AI response",
            IOptions<SemanticKernelSettings>? settings = null)
    {
        var mockChat = new Mock<IChatCompletionService>();

        // Mock GetChatMessageContentsAsync (the real interface method, not the extension).
        // The extension method GetChatMessageContentAsync calls this internally.
        mockChat
            .Setup(s => s.GetChatMessageContentsAsync(
                It.IsAny<ChatHistory>(),
                It.IsAny<PromptExecutionSettings?>(),
                It.IsAny<Kernel?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChatMessageContent>
            {
                new(AuthorRole.Assistant, chatResponse)
            });

        var kernel = Kernel.CreateBuilder().Build();
        var logger = new Mock<ILogger<PharmacyAssistantService>>();
        var resolvedSettings = settings ?? CreateSettings();

        var service = new PharmacyAssistantService(kernel, mockChat.Object, resolvedSettings, logger.Object);
        return (service, mockChat);
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "AI")]
    public async Task ChatAsync_WhenCalled_ReturnsMockedResponse()
    {
        // Arrange
        const string expectedResponse = "Amoxicillin is an antibiotic used to treat bacterial infections.";
        var (service, _) = CreateService(chatResponse: expectedResponse);

        // Act
        var result = await service.ChatAsync(
            userId: "user-123",
            history: [],
            userMessage: "What is Amoxicillin?");

        // Assert
        Assert.Equal(expectedResponse, result);
    }

    [Fact]
    [Trait("Category", "AI")]
    public async Task ChatAsync_WhenChatServiceThrows_ReturnsGracefulFallback()
    {
        // Arrange
        var mockChat = new Mock<IChatCompletionService>();
        mockChat
            .Setup(s => s.GetChatMessageContentsAsync(
                It.IsAny<ChatHistory>(),
                It.IsAny<PromptExecutionSettings?>(),
                It.IsAny<Kernel?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Network error"));

        var kernel = Kernel.CreateBuilder().Build();

        var logger = new Mock<ILogger<PharmacyAssistantService>>();
        var service = new PharmacyAssistantService(kernel, mockChat.Object, CreateSettings(), logger.Object);

        // Act — should NOT throw
        var result = await service.ChatAsync("user-123", [], "Hello");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("sorry", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "AI")]
    public async Task ChatAsync_PassesHistoryToUnderlyingService()
    {
        // Arrange
        var (service, mockChat) = CreateService();

        var history = new List<ChatMessage>
        {
            ChatMessage.FromUser("What is Ibuprofen?"),
            ChatMessage.FromAssistant("Ibuprofen is an NSAID used to relieve pain and inflammation.")
        };

        // Act
        await service.ChatAsync("user-123", history, "What is the recommended dose?");

        // Assert: verify the interface method was called with a ChatHistory containing
        // the prior messages + the new user message (total = 3 messages + system prompt)
        mockChat.Verify(
            s => s.GetChatMessageContentsAsync(
                It.Is<ChatHistory>(h => h.Count >= 3), // system + 2 history + new user msg
                It.IsAny<PromptExecutionSettings?>(),
                It.IsAny<Kernel?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Category", "AI")]
    public async Task ChatAsync_WhenHistoryExceedsMaxTurns_TrimsHistory()
    {
        // Arrange
        var settings = CreateSettings(maxHistoryTurns: 3);
        var (service, mockChat) = CreateService(settings: settings);

        // Create 10 history messages (more than MaxHistoryTurns = 3)
        var history = Enumerable.Range(1, 10)
            .Select(i => i % 2 == 0
                ? ChatMessage.FromUser($"Message {i}")
                : ChatMessage.FromAssistant($"Response {i}"))
            .ToList();

        // Act
        await service.ChatAsync("user-123", history, "New question");

        // Assert: system prompt (1) + trimmed history (max 3) + new user message (1) = max 5
        mockChat.Verify(
            s => s.GetChatMessageContentsAsync(
                It.Is<ChatHistory>(h => h.Count <= 5),
                It.IsAny<PromptExecutionSettings?>(),
                It.IsAny<Kernel?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Category", "AI")]
    public async Task ChatAsync_SystemPromptContainsCurrentDate()
    {
        // Arrange
        ChatHistory? capturedHistory = null;
        var mockChat = new Mock<IChatCompletionService>();
        mockChat
            .Setup(s => s.GetChatMessageContentsAsync(
                It.IsAny<ChatHistory>(),
                It.IsAny<PromptExecutionSettings?>(),
                It.IsAny<Kernel?>(),
                It.IsAny<CancellationToken>()))
            .Callback<ChatHistory, PromptExecutionSettings?, Kernel?, CancellationToken>(
                (h, _, _, _) => capturedHistory = h)
            .ReturnsAsync(new List<ChatMessageContent>
            {
                new(AuthorRole.Assistant, "OK")
            });

        var kernel = Kernel.CreateBuilder().Build();

        var service = new PharmacyAssistantService(
            kernel, mockChat.Object, CreateSettings(),
            new Mock<ILogger<PharmacyAssistantService>>().Object);

        // Act
        await service.ChatAsync("user-42", [], "Hello");

        // Assert: the system prompt (first message) should contain today's date
        Assert.NotNull(capturedHistory);
        var systemMsg = capturedHistory!.First();
        Assert.Equal(AuthorRole.System, systemMsg.Role);
        Assert.Contains(DateTime.UtcNow.Year.ToString(), systemMsg.Content ?? "");
    }

    [Fact]
    [Trait("Category", "AI")]
    public async Task ChatStreamAsync_WhenCalled_YieldsChunks()
    {
        // Arrange
        var mockChat = new Mock<IChatCompletionService>();

        var chunks = new List<StreamingChatMessageContent>
        {
            new(AuthorRole.Assistant, "Amox"),
            new(AuthorRole.Assistant, "icillin"),
            new(AuthorRole.Assistant, " is an antibiotic.")
        };

        // GetStreamingChatMessageContentsAsync IS the real interface method (not extension).
        mockChat
            .Setup(s => s.GetStreamingChatMessageContentsAsync(
                It.IsAny<ChatHistory>(),
                It.IsAny<PromptExecutionSettings?>(),
                It.IsAny<Kernel?>(),
                It.IsAny<CancellationToken>()))
            .Returns(chunks.ToAsyncEnumerable());

        var kernel = Kernel.CreateBuilder().Build();

        var service = new PharmacyAssistantService(
            kernel, mockChat.Object, CreateSettings(),
            new Mock<ILogger<PharmacyAssistantService>>().Object);

        // Act
        var received = new List<string>();
        await foreach (var chunk in service.ChatStreamAsync("user-1", [], "What is Amoxicillin?"))
        {
            received.Add(chunk);
        }

        // Assert
        Assert.Equal(3, received.Count);
        Assert.Equal("Amox", received[0]);
        Assert.Equal("icillin", received[1]);
        Assert.Equal(" is an antibiotic.", received[2]);
    }
}

// Extension helper for tests
file static class AsyncEnumerableExtensions
{
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> source)
    {
        foreach (var item in source)
        {
            yield return item;
            await Task.Yield();
        }
    }
}
