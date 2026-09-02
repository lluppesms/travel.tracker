namespace TravelTracker.Services.Interfaces;

/// <summary>
/// Provider-neutral handle for one Copilot SDK session.
/// </summary>
public interface ICopilotSessionHandle : IAsyncDisposable
{
    /// <summary>Gets the SDK session identifier.</summary>
    string SessionId { get; }

    /// <summary>Sends one non-streaming turn and returns the final assistant text.</summary>
    Task<string?> SendAndWaitAsync(
        string prompt,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);
}
