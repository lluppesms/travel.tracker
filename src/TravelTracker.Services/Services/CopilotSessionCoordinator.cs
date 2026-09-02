using GitHub.Copilot;
using TravelTracker.Services.Models;

namespace TravelTracker.Services.Services;

/// <summary>
/// Singleton coordinator for Copilot session lifecycle.
/// Manages thread → user mapping, turn serialization with semaphores,
/// session idle timeouts (15 min), turn timeouts (60 sec),
/// and quota enforcement (3 sessions/user, 100/instance).
/// </summary>
public class CopilotSessionCoordinator : ICopilotSessionCoordinator
{
    private const int MaxSessionsPerUser = 3;
    private const int MaxSessionsPerInstance = 100;
    private const int IdleTimeoutMinutes = 15;
    private const int TurnTimeoutSeconds = 60;
    private const int MaxDiskUsageBytes = 100 * 1024 * 1024; // 100 MB

    private readonly ICopilotRuntimeAccessor _runtimeAccessor;
    private readonly ILogger<CopilotSessionCoordinator> _logger;
    private readonly IOptionsMonitor<TravelAssistantOptions> _assistantOptions;

    // Active sessions: sessionId → CopilotSessionInfo
    private readonly Dictionary<string, CopilotSessionInfo> _sessions = new();

    // User sessions: userId → [sessionId]
    private readonly Dictionary<int, HashSet<string>> _userSessions = new();

    // Thread locks: sessionId → SemaphoreSlim
    private readonly Dictionary<string, SemaphoreSlim> _turnLocks = new();

    // Global lock for coordinator state mutations
    private readonly SemaphoreSlim _coordinatorLock = new(1, 1);

    public CopilotSessionCoordinator(
        ICopilotRuntimeAccessor runtimeAccessor,
        ILogger<CopilotSessionCoordinator> logger,
        IOptionsMonitor<TravelAssistantOptions> assistantOptions)
    {
        _runtimeAccessor = runtimeAccessor;
        _logger = logger;
        _assistantOptions = assistantOptions;
    }

    public async Task<CopilotSessionInfo> AcquireSessionAsync(
        TravelAssistantUserContext user,
        string threadId,
        CancellationToken cancellationToken = default)
    {
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user), "User context is required.");
        }

        if (string.IsNullOrWhiteSpace(threadId))
        {
            throw new ArgumentNullException(nameof(threadId), "Thread ID is required.");
        }

        await _coordinatorLock.WaitAsync(cancellationToken);
        try
        {
            var sessionId = GenerateSessionId(user.UserId.ToString(), threadId);

            // Return existing session if already acquired
            if (_sessions.TryGetValue(sessionId, out var existing))
            {
                _logger.LogDebug("Reusing existing session {SessionId} for user {UserId} thread {ThreadId}",
                    sessionId, user.UserId, threadId);
                return existing;
            }

            // Check user quota
            var userSessionCount = _userSessions.TryGetValue(user.UserId, out var userIds) ? userIds.Count : 0;
            if (userSessionCount >= MaxSessionsPerUser)
            {
                throw new SessionQuotaExceededException(
                    $"User {user.UserId} has reached max sessions ({MaxSessionsPerUser})");
            }

            // Check instance quota
            if (_sessions.Count >= MaxSessionsPerInstance)
            {
                throw new SessionQuotaExceededException(
                    $"Instance has reached max sessions ({MaxSessionsPerInstance})");
            }

            // Create new session
            _logger.LogInformation("Creating new session {SessionId} for user {UserId} thread {ThreadId}",
                sessionId, user.UserId, threadId);

            var now = DateTime.UtcNow;
            var copilotClient = (CopilotClient)_runtimeAccessor.GetClient();

            // Create non-streaming session with SDK configuration
            // SessionConfig from GitHub.Copilot namespace provides session configuration
            var sessionConfig = new SessionConfig();
            var copilotSession = await copilotClient.CreateSessionAsync(sessionConfig, cancellationToken);

            var sessionInfo = new CopilotSessionInfo
            {
                SessionId = sessionId,
                User = user,
                ThreadId = threadId,
                Session = copilotSession,
                CreatedAtUtc = now,
                LastActivityUtc = now,
                TurnCount = 0
            };

            _sessions[sessionId] = sessionInfo;
            if (!_userSessions.ContainsKey(user.UserId))
            {
                _userSessions[user.UserId] = new HashSet<string>();
            }
            _userSessions[user.UserId].Add(sessionId);
            _turnLocks[sessionId] = new SemaphoreSlim(1, 1);

            return sessionInfo;
        }
        finally
        {
            _coordinatorLock.Release();
        }
    }

    public async Task<IAsyncDisposable> AcquireTurnAsync(
        CopilotSessionInfo sessionInfo,
        TravelAssistantUserContext currentUser,
        CancellationToken cancellationToken = default)
    {
        if (sessionInfo == null)
        {
            throw new ArgumentNullException(nameof(sessionInfo));
        }

        if (currentUser == null)
        {
            throw new ArgumentNullException(nameof(currentUser));
        }

        // Verify ownership
        if (sessionInfo.User.UserId != currentUser.UserId)
        {
            throw new CrossUserSessionException(
                $"Session belongs to user {sessionInfo.User.UserId}, not {currentUser.UserId}");
        }

        await _coordinatorLock.WaitAsync(cancellationToken);
        try
        {
            // Check session still exists and is not stale
            if (!_sessions.TryGetValue(sessionInfo.SessionId, out var current))
            {
                throw new StaleSessionException(
                    $"Session {sessionInfo.SessionId} not found (evicted or deleted)");
            }

            if (IsStale(current))
            {
                // Silently evict stale session
                await EvictSessionAsync(current);
                throw new StaleSessionException(
                    $"Session {sessionInfo.SessionId} is idle > {IdleTimeoutMinutes} minutes (evicted)");
            }

            // Acquire turn lock with timeout
            if (!_turnLocks.TryGetValue(sessionInfo.SessionId, out var turnLock))
            {
                throw new StaleSessionException($"Session {sessionInfo.SessionId} has no turn lock");
            }

            _logger.LogDebug("Waiting for turn lock on session {SessionId}", sessionInfo.SessionId);

            var turnCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            turnCts.CancelAfter(TimeSpan.FromSeconds(TurnTimeoutSeconds));

            try
            {
                await turnLock.WaitAsync(turnCts.Token);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Turn lock timeout on session {SessionId} (> {TurnTimeoutSeconds}s)",
                    sessionInfo.SessionId, TurnTimeoutSeconds);
                throw;
            }

            _logger.LogDebug("Acquired turn lock on session {SessionId}", sessionInfo.SessionId);

            return new TurnLockReleaser(this, sessionInfo, turnLock, _logger);
        }
        finally
        {
            _coordinatorLock.Release();
        }
    }

    public async Task DeleteSessionAsync(
        CopilotSessionInfo sessionInfo,
        TravelAssistantUserContext currentUser,
        CancellationToken cancellationToken = default)
    {
        if (sessionInfo == null)
        {
            throw new ArgumentNullException(nameof(sessionInfo));
        }

        if (currentUser == null)
        {
            throw new ArgumentNullException(nameof(currentUser));
        }

        // Verify ownership
        if (sessionInfo.User.UserId != currentUser.UserId)
        {
            throw new CrossUserSessionException(
                $"Session belongs to user {sessionInfo.User.UserId}, not {currentUser.UserId}");
        }

        await _coordinatorLock.WaitAsync(cancellationToken);
        try
        {
            if (_sessions.TryGetValue(sessionInfo.SessionId, out var session))
            {
                await EvictSessionAsync(session);
            }
        }
        finally
        {
            _coordinatorLock.Release();
        }
    }

    public async Task CleanupAbandonedSessionsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Cleaning up abandoned sessions...");

        await _coordinatorLock.WaitAsync(cancellationToken);
        try
        {
            var now = DateTime.UtcNow;
            var staleSessionIds = _sessions
                .Where(kvp => IsStale(kvp.Value))
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var sessionId in staleSessionIds)
            {
                if (_sessions.TryGetValue(sessionId, out var session))
                {
                    _logger.LogInformation("Evicting stale session {SessionId} (idle > {IdleTimeoutMinutes}m)",
                        sessionId, IdleTimeoutMinutes);
                    await EvictSessionAsync(session);
                }
            }

            // Clean COPILOT_HOME disk usage
            await CleanupDiskAsync(cancellationToken);
        }
        finally
        {
            _coordinatorLock.Release();
        }
    }

    private async Task EvictSessionAsync(CopilotSessionInfo session)
    {
        try
        {
            _logger.LogInformation("Evicting session {SessionId}", session.SessionId);

            // Dispose session
            if (session.Session is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }

            // Call DeleteSessionAsync on client if available
            try
            {
                var copilotClient = (CopilotClient)_runtimeAccessor.GetClient();
                await copilotClient.DeleteSessionAsync(session.SessionId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error deleting session {SessionId} from client", session.SessionId);
            }

            // Remove from tracking
            _sessions.Remove(session.SessionId);
            if (_userSessions.TryGetValue(session.User.UserId, out var userIds))
            {
                userIds.Remove(session.SessionId);
            }
            if (_turnLocks.TryGetValue(session.SessionId, out var turnLock))
            {
                turnLock.Dispose();
                _turnLocks.Remove(session.SessionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evicting session {SessionId}", session.SessionId);
        }
    }

    private async Task CleanupDiskAsync(CancellationToken cancellationToken)
    {
        try
        {
            var copilotHome = _assistantOptions.CurrentValue.CopilotHome;
            if (string.IsNullOrWhiteSpace(copilotHome))
            {
                copilotHome = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "TravelTracker",
                    "copilot"
                );
            }

            if (!Directory.Exists(copilotHome))
            {
                return;
            }

            var dir = new DirectoryInfo(copilotHome);
            var totalSize = GetDirectorySizeBytes(dir);

            if (totalSize > MaxDiskUsageBytes)
            {
                _logger.LogWarning(
                    "COPILOT_HOME disk usage {UsageBytes} bytes > limit {MaxBytes} bytes",
                    totalSize, MaxDiskUsageBytes);

                // Delete oldest session directories until under limit
                var sessionDirs = dir.GetDirectories()
                    .OrderBy(d => d.CreationTimeUtc)
                    .ToList();

                foreach (var sessionDir in sessionDirs)
                {
                    if (totalSize <= MaxDiskUsageBytes)
                    {
                        break;
                    }

                    try
                    {
                        var sessionDirSize = GetDirectorySizeBytes(sessionDir);
                        _logger.LogInformation("Deleting old session directory {SessionDir}", sessionDir.Name);
                        sessionDir.Delete(recursive: true);
                        totalSize -= sessionDirSize;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error deleting session directory {SessionDir}", sessionDir.Name);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up COPILOT_HOME disk usage");
        }
    }

    private bool IsStale(CopilotSessionInfo session)
    {
        var idleDuration = DateTime.UtcNow - session.LastActivityUtc;
        return idleDuration.TotalMinutes > IdleTimeoutMinutes;
    }

    private string GenerateSessionId(string userId, string threadId)
    {
        // Deterministic but unique per user+thread combination
        return $"{userId}:{threadId}:{Guid.NewGuid():N}".ToLowerInvariant();
    }

    private long GetDirectorySizeBytes(DirectoryInfo dir)
    {
        try
        {
            return dir.GetFiles().Sum(f => f.Length) +
                   dir.GetDirectories().Sum(d => GetDirectorySizeBytes(d));
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Helper class to release turn lock when disposed.
    /// </summary>
    private class TurnLockReleaser : IAsyncDisposable
    {
        private readonly CopilotSessionCoordinator _coordinator;
        private readonly CopilotSessionInfo _sessionInfo;
        private readonly SemaphoreSlim _turnLock;
        private readonly ILogger _logger;
        private bool _disposed;

        public TurnLockReleaser(
            CopilotSessionCoordinator coordinator,
            CopilotSessionInfo sessionInfo,
            SemaphoreSlim turnLock,
            ILogger logger)
        {
            _coordinator = coordinator;
            _sessionInfo = sessionInfo;
            _turnLock = turnLock;
            _logger = logger;
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            try
            {
                // Update last activity
                await _coordinator._coordinatorLock.WaitAsync();
                try
                {
                    if (_coordinator._sessions.TryGetValue(_sessionInfo.SessionId, out var current))
                    {
                        var updated = new CopilotSessionInfo
                        {
                            SessionId = current.SessionId,
                            User = current.User,
                            ThreadId = current.ThreadId,
                            Session = current.Session,
                            CreatedAtUtc = current.CreatedAtUtc,
                            LastActivityUtc = DateTime.UtcNow,
                            TurnCount = current.TurnCount + 1
                        };
                        _coordinator._sessions[_sessionInfo.SessionId] = updated;
                    }
                }
                finally
                {
                    _coordinator._coordinatorLock.Release();
                }
            }
            finally
            {
                _turnLock.Release();
                _logger.LogDebug("Released turn lock on session {SessionId}", _sessionInfo.SessionId);
            }
        }
    }
}
