using Microsoft.Extensions.Options;
using TravelTracker.Services.Models;

namespace TravelTracker.Services.Services;

/// <summary>
/// Executes bounded, non-streaming chat turns through the Copilot SDK.
/// </summary>
public sealed class CopilotChatbotService(
    ICopilotSessionCoordinator sessionCoordinator,
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
            var session = await sessionCoordinator.AcquireSessionAsync(
                user,
                effectiveThreadId,
                createIfMissing: string.IsNullOrWhiteSpace(threadId),
                cancellationToken).ConfigureAwait(false);

            if (session.TurnCount >= assistantOptions.CurrentValue.MaxTurnsPerSession)
            {
                return ChatTurnResult.Failure(
                    ChatErrorCodes.RateLimited,
                    "This conversation has reached its turn limit. Please start a new conversation.",
                    effectiveThreadId);
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
                    effectiveThreadId);
            }

            return ChatTurnResult.Success(response.Trim(), effectiveThreadId, timeProvider.GetUtcNow());
        }
        catch (CrossUserSessionException exception)
        {
            logger.LogWarning(exception, "Rejected cross-user Copilot thread access for user {UserId}.", userId);
            return ChatTurnResult.Failure(
                ChatErrorCodes.Forbidden,
                "You are not allowed to access this conversation.",
                effectiveThreadId);
        }
        catch (StaleSessionException exception)
        {
            logger.LogInformation(exception, "Rejected stale or unknown Copilot thread for user {UserId}.", userId);
            return ChatTurnResult.Failure(
                ChatErrorCodes.ThreadNotFound,
                "This conversation is no longer available. Please start a new conversation.",
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
