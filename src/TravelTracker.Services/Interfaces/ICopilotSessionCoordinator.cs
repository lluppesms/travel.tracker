using TravelTracker.Services.Models;

namespace TravelTracker.Services.Interfaces;

/// <summary>
/// Singleton coordinator for Copilot session lifecycle and quotas.
/// Maps thread IDs to authenticated users, enforces turn serialization,
/// manages session idle/quota limits, and enables safe multi-user, multi-session operations.
/// </summary>
public interface ICopilotSessionCoordinator
{
    /// <summary>
    /// Acquires or creates a session for the authenticated user on the specified thread.
    /// </summary>
    /// <remarks>
    /// Enforces:
    /// - User can have max 3 active sessions
    /// - Instance can have max 100 active sessions
    /// - Session belongs exclusively to the authenticated user
    /// - Sessions are lazily created on first access
    /// </remarks>
    /// <param name="user">Authenticated user context.</param>
    /// <param name="threadId">Thread identifier (must be unique per user).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Session info with exclusive user binding.</returns>
    /// <exception cref="SessionQuotaExceededException">User or instance quota exceeded.</exception>
    /// <exception cref="ArgumentNullException">User or threadId is null/empty.</exception>
    Task<CopilotSessionInfo> AcquireSessionAsync(
        TravelAssistantUserContext user,
        string threadId,
        bool createIfMissing = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Acquires an exclusive turn lock for a session with a timeout.
    /// </summary>
    /// <remarks>
    /// Enforces:
    /// - Session belongs to the authenticated user (cross-user access rejected)
    /// - Session is not stale (idle > 15 minutes, evicted)
    /// - Turn completes within 60 seconds or is forcibly terminated
    /// - Only one turn executes per session at a time
    /// </remarks>
    /// <param name="sessionInfo">Session to lock.</param>
    /// <param name="currentUser">Current authenticated user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Async disposable lock. Release to end the turn.</returns>
    /// <exception cref="CrossUserSessionException">Session belongs to different user.</exception>
    /// <exception cref="StaleSessionException">Session is idle > 15 minutes (evicted).</exception>
    Task<ICopilotTurnLease> AcquireTurnAsync(
        CopilotSessionInfo sessionInfo,
        TravelAssistantUserContext currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes and deletes a session, removing it from active tracking.
    /// </summary>
    /// <remarks>
    /// This is idempotent. The session is disposed and COPILOT_HOME files are cleaned.
    /// Does NOT throw if called on already-deleted or non-existent sessions.
    /// </remarks>
    /// <param name="sessionInfo">Session to delete.</param>
    /// <param name="currentUser">Current authenticated user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="CrossUserSessionException">Session belongs to different user.</exception>
    Task DeleteSessionAsync(
        CopilotSessionInfo sessionInfo,
        TravelAssistantUserContext currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up abandoned sessions at startup:
    /// - Sessions idle > 15 minutes are deleted
    /// - Disk files in COPILOT_HOME are cleaned
    /// - Total disk use is capped at a reasonable limit (e.g., 100 MB)
    /// </summary>
    /// <remarks>
    /// Called once during app startup (before runtime is available for new sessions).
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CleanupAbandonedSessionsAsync(CancellationToken cancellationToken = default);
}
