using GitHub.Copilot;

namespace TravelTracker.Services.Interfaces;

/// <summary>
/// Provides thread-safe access to the singleton Copilot runtime.
/// </summary>
public interface ICopilotRuntimeAccessor
{
    /// <summary>
    /// Gets a value indicating whether the runtime is ready (started and healthy).
    /// </summary>
    bool IsReady { get; }

    /// <summary>
    /// Creates an isolated SDK session.
    /// </summary>
    Task<ICopilotSessionHandle> CreateSessionAsync(
        SessionConfig config,
        CancellationToken cancellationToken = default);

    /// <summary>Permanently deletes SDK session state.</summary>
    Task DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously starts the runtime once.
    /// Subsequent calls are no-op.
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a health check on the runtime.
    /// </summary>
    /// <returns>True if runtime is healthy; false otherwise.</returns>
    Task<bool> PingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the runtime gracefully within 10 seconds.
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Force-stops the runtime.
    /// </summary>
    Task ForceStopAsync();
}
