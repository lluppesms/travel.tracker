using GitHub.Copilot;

namespace TravelTracker.Services.Models;

/// <summary>
/// Immutable session metadata for a Copilot session bound to a specific user.
/// </summary>
public class CopilotSessionInfo
{
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
    public required CopilotSession Session { get; init; }

    /// <summary>
    /// When the session was created (UTC).
    /// </summary>
    public DateTime CreatedAtUtc { get; init; }

    /// <summary>
    /// When the session was last used (UTC).
    /// Updated on every turn or interaction.
    /// </summary>
    public DateTime LastActivityUtc { get; init; }

    /// <summary>
    /// Total turns (messages) in this session.
    /// </summary>
    public int TurnCount { get; init; }
}
