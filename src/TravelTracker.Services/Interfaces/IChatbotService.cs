using TravelTracker.Services.Models;

namespace TravelTracker.Services.Interfaces;

/// <summary>
/// Provider-neutral contract for a single assistant chat turn.
/// Confirmation and cancellation of pending actions are intentionally outside this interface;
/// a separate provider-neutral confirmation service owns that workflow.
/// </summary>
public interface IChatbotService
{
    /// <summary>
    /// Processes one user message and returns a structured, user-safe result.
    /// Implementations must never surface endpoint URLs, deployment names, exception messages, or stack traces.
    /// </summary>
    Task<ChatTurnResult> GetChatResponseAsync(
        string userMessage,
        int userId,
        string? threadId = null,
        DateTimeOffset? lastMessageDate = null,
        CancellationToken cancellationToken = default);
}
