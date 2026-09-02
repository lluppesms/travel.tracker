using GitHub.Copilot;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TravelTracker.Data.Configuration;
using TravelTracker.Data.Models;
using TravelTracker.Data.Repositories;
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
    public async Task GetChatResponseAsync_UnknownSuppliedThread_ReplacesThreadAndProcessesMessage()
    {
        var fixture = new ChatbotFixture();

        var result = await fixture.Service.GetChatResponseAsync("Hello", 7, "stale-thread");

        Assert.True(result.IsSuccess);
        Assert.Equal(ChatThreadStatuses.ThreadReplaced, result.ThreadStatus);
        Assert.NotEqual("stale-thread", result.ThreadId);
        Assert.Equal("Safe response", result.Message);
    }

    [Fact]
    public async Task GetChatResponseAsync_WhenToolPreparesAction_ReturnsNewestPendingAction()
    {
        var fixture = new ChatbotFixture();
        var actionId = Guid.NewGuid().ToString("N");
        fixture.ActionService
            .Setup(service => service.GetPendingActionsAsync(
                It.Is<TravelAssistantUserContext>(user => user.UserId == 7),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new AssistantActionSummary
                {
                    ActionId = actionId,
                    Summary = "Add Buffalo House RV Park for 2026-08-31",
                    CreatedAtUtc = DateTime.UtcNow,
                    ExpiresAtUtc = DateTime.UtcNow.AddHours(1),
                    State = AssistantActionStates.Pending
                }
            ]);

        var result = await fixture.Service.GetChatResponseAsync("Add Buffalo House for yesterday", 7);

        Assert.NotNull(result.PendingAction);
        Assert.Equal(actionId, result.PendingAction.ActionId);
        Assert.Equal("Add Buffalo House RV Park for 2026-08-31", result.PendingAction.Summary);
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
            ActionService
                .Setup(service => service.GetPendingActionsAsync(
                    It.IsAny<TravelAssistantUserContext>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
            UserService
                .Setup(service => service.GetUserByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(new User { Id = 7, Username = "traveler", Email = "traveler@example.com" });
            LocationSummaryRepository
                .Setup(repository => repository.GetLocationSummaryTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("## Locations_Visited\ncity=Somewhere");
            Service = new CopilotChatbotService(
                coordinator,
                ActionService.Object,
                UserService.Object,
                LocationSummaryRepository.Object,
                monitor.Object,
                NullLogger<CopilotChatbotService>.Instance,
                TimeProvider.System);
        }

        public RecordingSessionHandle Handle { get; } = new();
        public Mock<ITravelAssistantActionService> ActionService { get; } = new();
        public Mock<IUserService> UserService { get; } = new();
        public Mock<ILocationSummaryRepository> LocationSummaryRepository { get; } = new();
        public CopilotChatbotService Service { get; }
    }

    private sealed class RecordingSessionHandle : ICopilotSessionHandle
    {
        public string SessionId { get; } = $"test-{Guid.NewGuid():N}";
        public string LastPrompt { get; private set; } = string.Empty;
        public Exception? Exception { get; set; }

        public Task<CopilotTurnResponse> SendAndWaitAsync(string prompt, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            LastPrompt = prompt;
            return Exception is null
                ? Task.FromResult(new CopilotTurnResponse
                {
                    Content = "Safe response",
                    ModelCallCount = 1,
                    InputTokens = 10,
                    OutputTokens = 5,
                    CacheReadTokens = 0,
                    CacheWriteTokens = 0,
                    TotalCost = 0.01
                })
                : Task.FromException<CopilotTurnResponse>(Exception);
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
