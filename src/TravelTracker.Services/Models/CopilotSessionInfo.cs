namespace TravelTracker.Services.Models;

/// <summary>
/// Immutable session metadata for a Copilot session bound to a specific user.
/// </summary>
public class CopilotSessionInfo
{
    private long _lastActivityUtcTicks;
    private int _turnCount;

    /// <summary>
    /// Unique session identifier (per user, per thread).
    /// </summary>
    public required string SessionId { get; init; }

    /// <summary>
    /// The authenticated user this session belongs to.
    /// </summary>
    public required TravelAssistantUserContext User { get; init; }

    /// <summary>
    /// The thread ID associated with this session.
    /// </summary>
    public required string ThreadId { get; init; }

    /// <summary>
    /// The underlying Copilot SDK session object.
    /// </summary>
    public required ICopilotSessionHandle Session { get; init; }

    /// <summary>
    /// When the session was created (UTC).
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; init; }

    /// <summary>
    /// When the session was last used (UTC).
    /// Updated on every turn or interaction.
    /// </summary>
    public DateTimeOffset LastActivityUtc
    {
        get => new(Interlocked.Read(ref _lastActivityUtcTicks), TimeSpan.Zero);
        internal set => Interlocked.Exchange(ref _lastActivityUtcTicks, value.UtcTicks);
    }

    /// <summary>
    /// Total turns (messages) in this session.
    /// </summary>
    public int TurnCount => Volatile.Read(ref _turnCount);

    internal void CompleteTurn(DateTimeOffset completedAtUtc)
    {
        LastActivityUtc = completedAtUtc;
        Interlocked.Increment(ref _turnCount);
    }
}
