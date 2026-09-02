using GitHub.Copilot;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TravelTracker.Data.Configuration;
using TravelTracker.Services.Interfaces;
using TravelTracker.Services.Models;
using TravelTracker.Services.Services;

namespace TravelTracker.Tests.Services;

public class CopilotChatbotServiceTests
{
    [Fact]
    public async Task GetChatResponseAsync_NewThread_SendsBoundedContextAndReturnsSdkText()
    {
        var fixture = new ChatbotFixture();

        var result = await fixture.Service.GetChatResponseAsync("Where did I stay?", 7);

        Assert.True(result.IsSuccess);
        Assert.Equal("Safe response", result.Message);
        Assert.NotEmpty(result.ThreadId);
        Assert.Contains("America/Chicago", fixture.Handle.LastPrompt, StringComparison.Ordinal);
        Assert.Contains("<user_message>", fixture.Handle.LastPrompt, StringComparison.Ordinal);
        Assert.Contains("Where did I stay?", fixture.Handle.LastPrompt, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetChatResponseAsync_UnknownSuppliedThread_ReturnsStableError()
    {
        var fixture = new ChatbotFixture();

        var result = await fixture.Service.GetChatResponseAsync("Hello", 7, "stale-thread");

        Assert.Equal(ChatErrorCodes.ThreadNotFound, result.ErrorCode);
        Assert.DoesNotContain("StaleSessionException", result.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetChatResponseAsync_SdkFailure_ReturnsProviderUnavailableWithoutDetails()
    {
        var fixture = new ChatbotFixture();
        fixture.Handle.Exception = new InvalidOperationException("secret endpoint failure");

        var result = await fixture.Service.GetChatResponseAsync("Hello", 7);

        Assert.Equal(ChatErrorCodes.ProviderUnavailable, result.ErrorCode);
        Assert.DoesNotContain("secret endpoint", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class ChatbotFixture
    {
        public ChatbotFixture()
        {
            var options = new TravelAssistantOptions
            {
                TimeZoneId = "America/Chicago",
                MaxPromptCharacters = 4000,
                MaxTurnsPerSession = 20,
                MaxSessionsPerUser = 3,
                MaxSessionsPerInstance = 100,
                SessionIdleTimeoutMinutes = 15,
                TurnTimeoutSeconds = 60
            };
            var monitor = new Mock<IOptionsMonitor<TravelAssistantOptions>>();
            monitor.SetupGet(value => value.CurrentValue).Returns(options);
            var runtime = new Mock<ICopilotRuntimeAccessor>();
            runtime
                .Setup(value => value.CreateSessionAsync(It.IsAny<SessionConfig>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Handle);
            var toolFactory = new Mock<ICopilotTravelToolFactory>();
            toolFactory.Setup(factory => factory.CreateTools(It.IsAny<TravelAssistantUserContext>(), It.IsAny<string>()))
                .Returns([]);
            var coordinator = new CopilotSessionCoordinator(
                runtime.Object,
                NullLogger<CopilotSessionCoordinator>.Instance,
                monitor.Object,
                toolFactory.Object,
                TimeProvider.System);
            Service = new CopilotChatbotService(
                coordinator,
                monitor.Object,
                NullLogger<CopilotChatbotService>.Instance,
                TimeProvider.System);
        }

        public RecordingSessionHandle Handle { get; } = new();
        public CopilotChatbotService Service { get; }
    }

    private sealed class RecordingSessionHandle : ICopilotSessionHandle
    {
        public string SessionId { get; } = $"test-{Guid.NewGuid():N}";
        public string LastPrompt { get; private set; } = string.Empty;
        public Exception? Exception { get; set; }

        public Task<string?> SendAndWaitAsync(string prompt, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            LastPrompt = prompt;
            return Exception is null
                ? Task.FromResult<string?>("Safe response")
                : Task.FromException<string?>(Exception);
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
