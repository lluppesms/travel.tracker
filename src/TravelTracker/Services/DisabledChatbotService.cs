using TravelTracker.Services.Models;

namespace TravelTracker.Services;

/// <summary>
/// Registered in place of a real chat provider when the travel assistant prerequisites are missing (OPS-008).
/// Keeps <c>/chat</c> and <c>POST /api/chatbot/message</c> activatable so callers receive a stable
/// <see cref="ChatErrorCodes.ProviderUnavailable"/> result instead of a dependency injection failure.
/// </summary>
public sealed class DisabledChatbotService : IChatbotService
{
    internal const string UserSafeMessage = "The travel assistant is not configured.";

    public Task<ChatTurnResult> GetChatResponseAsync(
        string userMessage,
        int userId,
        string? threadId = null,
        DateTimeOffset? lastMessageDate = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(ChatTurnResult.Failure(
            ChatErrorCodes.ProviderUnavailable,
            UserSafeMessage,
            threadId ?? string.Empty));
    }
}
