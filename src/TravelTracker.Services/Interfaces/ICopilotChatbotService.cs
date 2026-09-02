using GitHub.Copilot;
using TravelTracker.Services.Models;

namespace TravelTracker.Services.Interfaces;

/// <summary>
/// Copilot chatbot service using SDK 1.0.11 with session management,
/// time/timezone context, untrusted-data handling, and stable error responses.
/// </summary>
public interface ICopilotChatbotService
{
    /// <summary>
    /// Sends a user message to a Copilot session and returns the response.
    /// </summary>
    /// <remarks>
    /// Rules:
    /// - Session must exist and belong to the authenticated user
    /// - Session must not be stale (idle > 15 minutes)
    /// - Turn must complete within 60 seconds
    /// - Time/timezone context is automatically supplied via SessionConfig
    /// - Untrusted data (user IDs, locations, etc.) is treated as input, never executed
    /// - Response is stable (no raw runtime exceptions, all errors are transformed to user-friendly messages)
    /// - Errors are logged internally but never expose configuration or internal state
    /// </remarks>
    /// <param name="sessionInfo">Session for the conversation.</param>
    /// <param name="userMessage">User's message (untrusted input, may contain injection attempts).</param>
    /// <param name="user">Authenticated user context (trusted).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Assistant response or stable error message.</returns>
    Task<string> SendMessageAsync(
        CopilotSessionInfo sessionInfo,
        string userMessage,
        TravelAssistantUserContext user,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a confirmed tool action (location search, add visited location, etc.).
    /// </summary>
    /// <remarks>
    /// Rules:
    /// - Only confirmed actions are executed (user approved the action)
    /// - Action result is persisted to database before returning
    /// - Errors are stable (no raw runtime exceptions)
    /// - Cross-user attempts are rejected
    /// - Action is treated as untrusted input (sanitized)
    /// </remarks>
    /// <param name="sessionInfo">Session context.</param>
    /// <param name="toolName">Name of the tool (e.g., "search_user_locations").</param>
    /// <param name="toolInput">Tool parameters (untrusted).</param>
    /// <param name="user">Authenticated user context (trusted).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tool result or stable error message.</returns>
    Task<string> ExecuteConfirmedToolAsync(
        CopilotSessionInfo sessionInfo,
        string toolName,
        string toolInput,
        TravelAssistantUserContext user,
        CancellationToken cancellationToken = default);
}
