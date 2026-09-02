using System.Security.Cryptography;
using System.Text;
using GitHub.Copilot;
using Microsoft.Extensions.Options;
using TravelTracker.Services.Models;

namespace TravelTracker.Services.Services;

/// <summary>
/// Coordinates bounded, user-owned Copilot sessions and serializes turns per thread.
/// </summary>
public sealed class CopilotSessionCoordinator(
    ICopilotRuntimeAccessor runtimeAccessor,
    ILogger<CopilotSessionCoordinator> logger,
    IOptionsMonitor<TravelAssistantOptions> assistantOptions,
    ICopilotTravelToolFactory toolFactory,
    TimeProvider timeProvider) : ICopilotSessionCoordinator
{
    private readonly Dictionary<string, CopilotSessionInfo> _sessionsByThread = new(StringComparer.Ordinal);
    private readonly Dictionary<string, SemaphoreSlim> _turnLocks = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _coordinatorLock = new(1, 1);

    public async Task<CopilotSessionInfo> AcquireSessionAsync(
        TravelAssistantUserContext user,
        string threadId,
        bool createIfMissing = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(threadId);

        await _coordinatorLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_sessionsByThread.TryGetValue(threadId, out var existing))
            {
                EnsureOwner(existing, user);
                if (IsStale(existing))
                {
                    await EvictSessionCoreAsync(existing, cancellationToken).ConfigureAwait(false);
                    throw new StaleSessionException("The session has expired.");
                }

                return existing;
            }

            if (!createIfMissing)
            {
                throw new StaleSessionException("The session does not exist.");
            }

            var options = assistantOptions.CurrentValue;
            var userSessionCount = _sessionsByThread.Values.Count(session => session.User.UserId == user.UserId);
            if (userSessionCount >= options.MaxSessionsPerUser ||
                _sessionsByThread.Count >= options.MaxSessionsPerInstance)
            {
                throw new SessionQuotaExceededException("The active session quota has been reached.");
            }

            var sessionId = CreateSessionId(user.UserId, threadId);
            var config = CreateSessionConfig(sessionId);
            config.Tools = toolFactory.CreateTools(user, threadId);
            config.AvailableTools = CopilotTravelToolNames.All
                .Select(toolName => $"custom:{toolName}")
                .ToArray();
            toolFactory.ConfigureSession(config);
            var handle = await runtimeAccessor.CreateSessionAsync(config, cancellationToken).ConfigureAwait(false);
            var now = timeProvider.GetUtcNow();
            var session = new CopilotSessionInfo
            {
                SessionId = handle.SessionId,
                User = user,
                ThreadId = threadId,
                Session = handle,
                CreatedAtUtc = now,
                LastActivityUtc = now
            };

            _sessionsByThread.Add(threadId, session);
            _turnLocks.Add(threadId, new SemaphoreSlim(1, 1));
            return session;
        }
        finally
        {
            _coordinatorLock.Release();
        }
    }

    public async Task<ICopilotTurnLease> AcquireTurnAsync(
        CopilotSessionInfo sessionInfo,
        TravelAssistantUserContext currentUser,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sessionInfo);
        ArgumentNullException.ThrowIfNull(currentUser);
        EnsureOwner(sessionInfo, currentUser);

        SemaphoreSlim turnLock;
        await _coordinatorLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!_sessionsByThread.TryGetValue(sessionInfo.ThreadId, out var current) ||
                !ReferenceEquals(current, sessionInfo))
            {
                throw new StaleSessionException("The session is no longer active.");
            }

            if (IsStale(current))
            {
                await EvictSessionCoreAsync(current, cancellationToken).ConfigureAwait(false);
                throw new StaleSessionException("The session has expired.");
            }

            turnLock = _turnLocks[current.ThreadId];
        }
        finally
        {
            _coordinatorLock.Release();
        }

        CancellationTokenSource? turnCts = null;
        try
        {
            await turnLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            turnCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            turnCts.CancelAfter(TimeSpan.FromSeconds(assistantOptions.CurrentValue.TurnTimeoutSeconds));
            return new TurnLease(this, sessionInfo, turnLock, turnCts);
        }
        catch
        {
            turnCts?.Dispose();
            throw;
        }
    }

    public async Task DeleteSessionAsync(
        CopilotSessionInfo sessionInfo,
        TravelAssistantUserContext currentUser,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sessionInfo);
        ArgumentNullException.ThrowIfNull(currentUser);
        EnsureOwner(sessionInfo, currentUser);

        SemaphoreSlim? turnLock;
        await _coordinatorLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            turnLock = _turnLocks.GetValueOrDefault(sessionInfo.ThreadId);
        }
        finally
        {
            _coordinatorLock.Release();
        }

        if (turnLock is null)
        {
            return;
        }

        await turnLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await _coordinatorLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_sessionsByThread.TryGetValue(sessionInfo.ThreadId, out var current) &&
                    ReferenceEquals(current, sessionInfo))
                {
                    await EvictSessionCoreAsync(current, cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                _coordinatorLock.Release();
            }
        }
        finally
        {
            turnLock.Release();
        }
    }

    public async Task CleanupAbandonedSessionsAsync(CancellationToken cancellationToken = default)
    {
        await _coordinatorLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            foreach (var session in _sessionsByThread.Values.Where(IsStale).ToArray())
            {
                if (_turnLocks[session.ThreadId].Wait(0))
                {
                    try
                    {
                        await EvictSessionCoreAsync(session, cancellationToken).ConfigureAwait(false);
                    }
                    finally
                    {
                        if (_turnLocks.TryGetValue(session.ThreadId, out var turnLock))
                        {
                            turnLock.Release();
                        }
                    }
                }
            }

            CleanupDisk();
        }
        finally
        {
            _coordinatorLock.Release();
        }
    }

    internal static SessionConfig CreateSessionConfig(string sessionId) => new()
    {
        SessionId = sessionId,
        Streaming = false,
        InfiniteSessions = new InfiniteSessionConfig { Enabled = false },
        Memory = new MemoryConfiguration { Enabled = false },
        EnableSessionStore = false,
        EnableConfigDiscovery = false,
        EnableOnDemandInstructionDiscovery = false,
        EnableFileHooks = false,
        EnableHostGitOperations = false,
        EnableSkills = false,
        SkipEmbeddingRetrieval = true,
        EmbeddingCacheStorage = EmbeddingCacheStorageMode.InMemory,
        McpOAuthTokenStorage = McpOAuthTokenStorageMode.InMemory,
        AvailableTools = [],
        SystemMessage = new SystemMessageConfig
        {
            Mode = SystemMessageMode.Replace,
            Content = CopilotSystemInstructions.Base
        }
    };

    private async Task EvictSessionCoreAsync(
        CopilotSessionInfo session,
        CancellationToken cancellationToken)
    {
        _sessionsByThread.Remove(session.ThreadId);
        _turnLocks.Remove(session.ThreadId);

        try
        {
            await session.Session.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            try
            {
                await runtimeAccessor.DeleteSessionAsync(session.SessionId, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Copilot session {SessionId} was already absent or could not be deleted.", session.SessionId);
            }
        }
    }

    private void CleanupDisk()
    {
        var home = assistantOptions.CurrentValue.CopilotHome;
        if (!Directory.Exists(home))
        {
            return;
        }

        var activeIds = _sessionsByThread.Values.Select(session => session.SessionId).ToHashSet(StringComparer.Ordinal);
        var sessionRoot = Path.Combine(home, "session-state");
        if (Directory.Exists(sessionRoot))
        {
            foreach (var directory in new DirectoryInfo(sessionRoot).EnumerateDirectories())
            {
                if (!activeIds.Contains(directory.Name))
                {
                    try
                    {
                        directory.Delete(recursive: true);
                    }
                    catch (IOException exception)
                    {
                        logger.LogWarning(exception, "Could not remove abandoned Copilot session state {SessionId}.", directory.Name);
                    }
                    catch (UnauthorizedAccessException exception)
                    {
                        logger.LogWarning(exception, "Could not access abandoned Copilot session state {SessionId}.", directory.Name);
                    }
                }
            }
        }

        if (!Directory.Exists(sessionRoot))
        {
            return;
        }

        var maxBytes = assistantOptions.CurrentValue.MaxCopilotHomeBytes;
        var files = new DirectoryInfo(sessionRoot).EnumerateFiles("*", SearchOption.AllDirectories)
            .OrderBy(file => file.LastWriteTimeUtc)
            .ToList();
        var totalBytes = files.Sum(file => file.Length);
        foreach (var file in files)
        {
            if (totalBytes <= maxBytes)
            {
                break;
            }

            if (activeIds.Any(id => file.FullName.Contains(id, StringComparison.Ordinal)))
            {
                continue;
            }

            try
            {
                var length = file.Length;
                file.Delete();
                totalBytes -= length;
            }
            catch (IOException exception)
            {
                logger.LogWarning(exception, "Could not trim Copilot state file {FileName}.", file.Name);
            }
            catch (UnauthorizedAccessException exception)
            {
                logger.LogWarning(exception, "Could not access Copilot state file {FileName}.", file.Name);
            }
        }
    }

    private bool IsStale(CopilotSessionInfo session)
        => timeProvider.GetUtcNow() - session.LastActivityUtc >=
           TimeSpan.FromMinutes(assistantOptions.CurrentValue.SessionIdleTimeoutMinutes);

    private void CompleteTurn(CopilotSessionInfo session)
        => session.CompleteTurn(timeProvider.GetUtcNow());

    private static void EnsureOwner(CopilotSessionInfo session, TravelAssistantUserContext user)
    {
        if (session.User.UserId != user.UserId)
        {
            throw new CrossUserSessionException("The session belongs to another user.");
        }
    }

    private static string CreateSessionId(int userId, string threadId)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{userId}:{threadId}"));
        return $"travel-{Convert.ToHexString(bytes).ToLowerInvariant()}";
    }

    private sealed class TurnLease(
        CopilotSessionCoordinator coordinator,
        CopilotSessionInfo session,
        SemaphoreSlim turnLock,
        CancellationTokenSource cancellationTokenSource) : ICopilotTurnLease
    {
        private bool _disposed;

        public CancellationToken CancellationToken => cancellationTokenSource.Token;

        public ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return ValueTask.CompletedTask;
            }

            _disposed = true;
            coordinator.CompleteTurn(session);
            cancellationTokenSource.Dispose();
            turnLock.Release();
            return ValueTask.CompletedTask;
        }
    }
}
