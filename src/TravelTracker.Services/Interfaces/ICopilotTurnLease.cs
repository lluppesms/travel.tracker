namespace TravelTracker.Services.Interfaces;

/// <summary>
/// Holds exclusive access to a session turn and supplies its bounded cancellation token.
/// </summary>
public interface ICopilotTurnLease : IAsyncDisposable
{
    /// <summary>Cancellation token that expires at the configured turn deadline.</summary>
    CancellationToken CancellationToken { get; }
}
