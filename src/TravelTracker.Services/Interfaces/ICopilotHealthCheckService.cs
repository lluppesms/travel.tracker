namespace TravelTracker.Services.Interfaces;

/// <summary>
/// Provides health check functionality for the Copilot runtime.
/// </summary>
public interface ICopilotHealthCheckService
{
    /// <summary>
    /// Checks if the Copilot runtime is healthy and ready.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the runtime is healthy; false otherwise.</returns>
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a human-readable reason if the runtime is not healthy.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of failure reasons if the runtime is not healthy; empty list if healthy.</returns>
    Task<IReadOnlyList<string>> GetFailureReasonsAsync(CancellationToken cancellationToken = default);
}
