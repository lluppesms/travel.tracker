using Microsoft.Extensions.Options;
using TravelTracker.Services.Models;

namespace TravelTracker.Services.Services;

/// <summary>
/// Executes bounded, non-streaming chat turns through the Copilot SDK.
/// </summary>
public sealed class CopilotChatbotService(
    ICopilotSessionCoordinator sessionCoordinator,
    ITravelAssistantActionService actionService,
    IOptionsMonitor<TravelAssistantOptions> assistantOptions,
    ILogger<CopilotChatbotService> logger,
    TimeProvider timeProvider) : IChatbotService
{
    public async Task<ChatTurnResult> GetChatResponseAsync(
        string userMessage,
        int userId,
        string? threadId = null,
        DateTimeOffset? lastMessageDate = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveThreadId = string.IsNullOrWhiteSpace(threadId)
            ? Guid.NewGuid().ToString("N")
            : threadId;

        if (string.IsNullOrWhiteSpace(userMessage) ||
            userMessage.Length > assistantOptions.CurrentValue.MaxPromptCharacters)
        {
            return ChatTurnResult.Failure(
                ChatErrorCodes.InvalidRequest,
                "Please provide a shorter message.",
                effectiveThreadId);
        }

        var user = new TravelAssistantUserContext(userId, string.Empty, string.Empty, string.Empty);
        try
        {
            try
            {
                return await ExecuteTurnAsync(
                    userMessage,
                    user,
                    effectiveThreadId,
                    createIfMissing: string.IsNullOrWhiteSpace(threadId),
                    threadReplaced: false,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (StaleSessionException exception)
            {
                effectiveThreadId = Guid.NewGuid().ToString("N");
                logger.LogInformation(exception, "Replaced stale or unknown Copilot thread for user {UserId}.", userId);
                return await ExecuteTurnAsync(
                    userMessage,
                    user,
                    effectiveThreadId,
                    createIfMissing: true,
                    threadReplaced: true,
                    cancellationToken).ConfigureAwait(false);
            }
        }
        catch (CrossUserSessionException exception)
        {
            logger.LogWarning(exception, "Rejected cross-user Copilot thread access for user {UserId}.", userId);
            return ChatTurnResult.Failure(
                ChatErrorCodes.Forbidden,
                "You are not allowed to access this conversation.",
                effectiveThreadId);
        }
        catch (SessionQuotaExceededException exception)
        {
            logger.LogInformation(exception, "Copilot session quota reached for user {UserId}.", userId);
            return ChatTurnResult.Failure(
                ChatErrorCodes.RateLimited,
                "Too many conversations are active. Please close one and try again.",
                effectiveThreadId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException exception)
        {
            logger.LogWarning(exception, "Copilot turn timed out for user {UserId}.", userId);
            return ChatTurnResult.Failure(
                ChatErrorCodes.ProviderUnavailable,
                "The travel assistant timed out. Please try again.",
                effectiveThreadId);
        }
        catch (TimeoutException exception)
        {
            logger.LogWarning(exception, "Copilot turn timed out for user {UserId}.", userId);
            return ChatTurnResult.Failure(
                ChatErrorCodes.ProviderUnavailable,
                "The travel assistant timed out. Please try again.",
                effectiveThreadId);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Copilot provider failed for user {UserId}.", userId);
            return ChatTurnResult.Failure(
                ChatErrorCodes.ProviderUnavailable,
                "The travel assistant is unavailable right now. Please try again later.",
                effectiveThreadId);
        }
    }

    private async Task<ChatTurnResult> ExecuteTurnAsync(
        string userMessage,
        TravelAssistantUserContext user,
        string threadId,
        bool createIfMissing,
        bool threadReplaced,
        CancellationToken cancellationToken)
    {
        var session = await sessionCoordinator.AcquireSessionAsync(
            user,
            threadId,
            createIfMissing,
            cancellationToken).ConfigureAwait(false);

        if (session.TurnCount >= assistantOptions.CurrentValue.MaxTurnsPerSession)
        {
            return ChatTurnResult.Failure(
                ChatErrorCodes.RateLimited,
                "This conversation has reached its turn limit. Please start a new conversation.",
                threadId);
        }

        await using var turn = await sessionCoordinator.AcquireTurnAsync(
            session,
            user,
            cancellationToken).ConfigureAwait(false);

        var timeout = TimeSpan.FromSeconds(assistantOptions.CurrentValue.TurnTimeoutSeconds);
        var response = await session.Session.SendAndWaitAsync(
            BuildTurnPrompt(userMessage),
            timeout,
            turn.CancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(response))
        {
            return ChatTurnResult.Failure(
                ChatErrorCodes.ProviderUnavailable,
                "The travel assistant did not return a response. Please try again.",
                threadId);
        }

        var pendingActions = await actionService
            .GetPendingActionsAsync(user, threadId, cancellationToken)
            .ConfigureAwait(false);
        var pendingAction = pendingActions.Count == 0 ? null : ToChatActionSummary(pendingActions[0]);
        var now = timeProvider.GetUtcNow();

        return threadReplaced
            ? ChatTurnResult.ThreadReplaced(response.Trim(), threadId, now, pendingAction: pendingAction)
            : ChatTurnResult.Success(response.Trim(), threadId, now, pendingAction: pendingAction);
    }

    private static ChatActionSummary ToChatActionSummary(AssistantActionSummary action) =>
        new()
        {
            ActionId = action.ActionId,
            ActionType = TravelAssistantActionService.ActionType,
            Title = "Add visited location",
            Summary = action.Summary,
            ExpiresAt = new DateTimeOffset(DateTime.SpecifyKind(action.ExpiresAtUtc, DateTimeKind.Utc))
        };

    private string BuildTurnPrompt(string userMessage)
    {
        var options = assistantOptions.CurrentValue;
        var timeZone = ResolveTimeZone(options.TimeZoneId);
        var localNow = TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), timeZone);

        return $"""
            Server-authoritative context:
            Current local date and time: {localNow:O}
            Time zone: {timeZone.Id}

            Untrusted user message:
            <user_message>
            {userMessage}
            </user_message>
            """;
    }

    private static TimeZoneInfo ResolveTimeZone(string timeZoneId)
    {
        if (TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out var timeZone))
        {
            return timeZone;
        }

        if (TimeZoneInfo.TryConvertIanaIdToWindowsId(timeZoneId, out var windowsId) &&
            TimeZoneInfo.TryFindSystemTimeZoneById(windowsId, out timeZone))
        {
            return timeZone;
        }

        throw new InvalidOperationException("The configured travel assistant time zone is unavailable.");
    }
}
