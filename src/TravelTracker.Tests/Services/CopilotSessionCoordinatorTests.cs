using GitHub.Copilot;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TravelTracker.Data.Configuration;
using TravelTracker.Services.Interfaces;
using TravelTracker.Services.Models;
using TravelTracker.Services.Services;

namespace TravelTracker.Tests.Services;

public class CopilotSessionCoordinatorTests
{
    [Fact]
    public async Task AcquireSessionAsync_SameThreadAndOwner_ReusesSession()
    {
        var fixture = new CoordinatorFixture();
        var user = CreateUser(1);

        var first = await fixture.Coordinator.AcquireSessionAsync(user, "thread-1", cancellationToken: TestContext.Current.CancellationToken);
        var second = await fixture.Coordinator.AcquireSessionAsync(user, "thread-1", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Same(first, second);
        fixture.Runtime.Verify(
            runtime => runtime.CreateSessionAsync(It.IsAny<SessionConfig>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AcquireSessionAsync_ExistingThreadForDifferentUser_RejectsAccess()
    {
        var fixture = new CoordinatorFixture();
        await fixture.Coordinator.AcquireSessionAsync(CreateUser(1), "shared-thread", cancellationToken: TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<CrossUserSessionException>(
            () => fixture.Coordinator.AcquireSessionAsync(CreateUser(2), "shared-thread", cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AcquireSessionAsync_UnknownExistingThread_RejectsStaleUse()
    {
        var fixture = new CoordinatorFixture();

        await Assert.ThrowsAsync<StaleSessionException>(
            () => fixture.Coordinator.AcquireSessionAsync(
                CreateUser(1),
                "unknown",
                createIfMissing: false,
                cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AcquireSessionAsync_UserQuotaReached_RejectsFourthSession()
    {
        var fixture = new CoordinatorFixture();
        var user = CreateUser(1);
        await fixture.Coordinator.AcquireSessionAsync(user, "one", cancellationToken: TestContext.Current.CancellationToken);
        await fixture.Coordinator.AcquireSessionAsync(user, "two", cancellationToken: TestContext.Current.CancellationToken);
        await fixture.Coordinator.AcquireSessionAsync(user, "three", cancellationToken: TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<SessionQuotaExceededException>(
            () => fixture.Coordinator.AcquireSessionAsync(user, "four", cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AcquireTurnAsync_ConcurrentTurns_AreSerialized()
    {
        var fixture = new CoordinatorFixture();
        var user = CreateUser(1);
        var session = await fixture.Coordinator.AcquireSessionAsync(user, "thread", cancellationToken: TestContext.Current.CancellationToken);
        await using var first = await fixture.Coordinator.AcquireTurnAsync(session, user, TestContext.Current.CancellationToken);

        var secondTask = fixture.Coordinator.AcquireTurnAsync(session, user, TestContext.Current.CancellationToken);
        await Task.Delay(50, TestContext.Current.CancellationToken);
        Assert.False(secondTask.IsCompleted);

        await first.DisposeAsync();
        await using var second = await secondTask;
    }

    [Fact]
    public async Task AcquireSessionAsync_AfterIdleTimeout_DisposesAndDeletesState()
    {
        var fixture = new CoordinatorFixture();
        var user = CreateUser(1);
        var session = await fixture.Coordinator.AcquireSessionAsync(user, "thread", cancellationToken: TestContext.Current.CancellationToken);
        fixture.Time.Advance(TimeSpan.FromMinutes(15));

        await Assert.ThrowsAsync<StaleSessionException>(
            () => fixture.Coordinator.AcquireSessionAsync(user, "thread", cancellationToken: TestContext.Current.CancellationToken));

        Assert.True(fixture.Handle.IsDisposed);
        fixture.Runtime.Verify(
            runtime => runtime.DeleteSessionAsync(session.SessionId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AcquireSessionAsync_ConfigDisablesPersistentAndUnlistedCapabilities()
    {
        var fixture = new CoordinatorFixture();
        SessionConfig? captured = null;
        fixture.Runtime
            .Setup(runtime => runtime.CreateSessionAsync(It.IsAny<SessionConfig>(), It.IsAny<CancellationToken>()))
            .Callback<SessionConfig, CancellationToken>((config, _) => captured = config)
            .ReturnsAsync(fixture.Handle);

        await fixture.Coordinator.AcquireSessionAsync(CreateUser(1), "thread", cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(captured);
        Assert.False(captured.Streaming);
        Assert.False(captured.InfiniteSessions!.Enabled);
        Assert.False(captured.Memory!.Enabled);
        Assert.False(captured.EnableSessionStore);
        Assert.False(captured.EnableConfigDiscovery);
        Assert.True(captured.SkipEmbeddingRetrieval);
        Assert.Equal(EmbeddingCacheStorageMode.InMemory, captured.EmbeddingCacheStorage);
        Assert.Equal(4, captured.AvailableTools!.Count);
        Assert.Contains("custom:prepare_add_visited_location", captured.AvailableTools);
        Assert.Equal(SystemMessageMode.Replace, captured.SystemMessage!.Mode);
    }

    [Fact]
    public async Task CleanupAbandonedSessionsAsync_RemovesOnlyAbandonedSessionState()
    {
        var fixture = new CoordinatorFixture();
        var sessionRoot = Directory.CreateDirectory(Path.Combine(fixture.Options.CopilotHome, "session-state"));
        var abandoned = sessionRoot.CreateSubdirectory("abandoned-session");
        await File.WriteAllTextAsync(Path.Combine(abandoned.FullName, "state.json"), "{}", TestContext.Current.CancellationToken);
        var runtimeFile = Path.Combine(fixture.Options.CopilotHome, "runtime-owned.txt");
        await File.WriteAllTextAsync(runtimeFile, "keep", TestContext.Current.CancellationToken);

        await fixture.Coordinator.CleanupAbandonedSessionsAsync(TestContext.Current.CancellationToken);

        Assert.False(Directory.Exists(abandoned.FullName));
        Assert.True(File.Exists(runtimeFile));
        Directory.Delete(fixture.Options.CopilotHome, recursive: true);
    }

    private static TravelAssistantUserContext CreateUser(int id)
        => new(id, $"external-{id}", $"User {id}", $"user{id}@example.com");

    private sealed class CoordinatorFixture
    {
        public CoordinatorFixture()
        {
            Options = new TravelAssistantOptions
            {
                CopilotHome = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")),
                MaxSessionsPerUser = 3,
                MaxSessionsPerInstance = 100,
                SessionIdleTimeoutMinutes = 15,
                TurnTimeoutSeconds = 2
            };
            var monitor = new Mock<IOptionsMonitor<TravelAssistantOptions>>();
            monitor.SetupGet(value => value.CurrentValue).Returns(Options);
            Handle = new FakeSessionHandle("sdk-session");
            Runtime = new Mock<ICopilotRuntimeAccessor>();
            Runtime
                .Setup(runtime => runtime.CreateSessionAsync(It.IsAny<SessionConfig>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Handle);
            var toolFactory = new Mock<ICopilotTravelToolFactory>();
            toolFactory.Setup(factory => factory.CreateTools(It.IsAny<TravelAssistantUserContext>(), It.IsAny<string>()))
                .Returns([]);
            Coordinator = new CopilotSessionCoordinator(
                Runtime.Object,
                NullLogger<CopilotSessionCoordinator>.Instance,
                monitor.Object,
                toolFactory.Object,
                Time);
        }

        public TravelAssistantOptions Options { get; }
        public MutableTimeProvider Time { get; } = new(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        public FakeSessionHandle Handle { get; }
        public Mock<ICopilotRuntimeAccessor> Runtime { get; }
        public CopilotSessionCoordinator Coordinator { get; }
    }

    private sealed class FakeSessionHandle(string sessionId) : ICopilotSessionHandle
    {
        public string SessionId { get; } = sessionId;
        public bool IsDisposed { get; private set; }

        public Task<CopilotTurnResponse> SendAndWaitAsync(string prompt, TimeSpan timeout, CancellationToken cancellationToken = default)
            => Task.FromResult(new CopilotTurnResponse { Content = "response" });

        public ValueTask DisposeAsync()
        {
            IsDisposed = true;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class MutableTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private DateTimeOffset _utcNow = utcNow;
        public override DateTimeOffset GetUtcNow() => _utcNow;
        public void Advance(TimeSpan duration) => _utcNow += duration;
    }
}
