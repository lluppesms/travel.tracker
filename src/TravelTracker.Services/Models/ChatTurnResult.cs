using System;
using System.Collections.Generic;

namespace TravelTracker.Services.Models;

/// <summary>
/// Structured result of a single assistant chat turn. Replaces the legacy
/// <c>(string message, DateTimeOffset? latestMessageDate, string threadId)</c> tuple contract.
/// All content is user-safe; confirmation of pending actions happens outside the provider interface.
/// </summary>
public sealed record ChatTurnResult
{
    /// <summary>Assistant message text to display to the user.</summary>
    public required string Message { get; init; }

    /// <summary>Timestamp of the latest thread message known to the server, used for incremental polling.</summary>
    public DateTimeOffset? LatestMessageDate { get; init; }

    /// <summary>Identifier of the thread that produced or should continue this conversation.</summary>
    public required string ThreadId { get; init; }

    /// <summary>User-safe per-tool statuses for this turn. Never null; defaults to empty.</summary>
    public IReadOnlyList<ToolStatus> ToolStatuses { get; init; } = Array.Empty<ToolStatus>();

    /// <summary>Pending action awaiting user confirmation, when the turn produced one.</summary>
    public ChatActionSummary? PendingAction { get; init; }

    /// <summary>Stable error code from <see cref="ChatErrorCodes"/>, or null when the turn succeeded.</summary>
    public string? ErrorCode { get; init; }

    /// <summary>Thread status from <see cref="ChatThreadStatuses"/>. Defaults to <see cref="ChatThreadStatuses.Active"/>.</summary>
    public string ThreadStatus { get; init; } = ChatThreadStatuses.Active;

    /// <summary>Diagnostic/usage information for this turn (duration, tokens, cost), when available.</summary>
    public ChatUsageInfo? Usage { get; init; }

    /// <summary>True when the turn completed without a stable error code.</summary>
    public bool IsSuccess => string.IsNullOrWhiteSpace(ErrorCode);

    /// <summary>True when the turn carries a stable error code.</summary>
    public bool IsError => !IsSuccess;

    /// <summary>HTTP status code that corresponds to this result.</summary>
    public int HttpStatusCode => ChatErrorCodes.ToHttpStatusCode(ErrorCode);

    /// <summary>Creates a successful chat turn result.</summary>
    public static ChatTurnResult Success(
        string message,
        string threadId,
        DateTimeOffset? latestMessageDate = null,
        IReadOnlyList<ToolStatus>? toolStatuses = null,
        ChatActionSummary? pendingAction = null,
        string? threadStatus = null,
        ChatUsageInfo? usage = null) =>
        new()
        {
            Message = message,
            ThreadId = threadId,
            LatestMessageDate = latestMessageDate,
            ToolStatuses = toolStatuses ?? Array.Empty<ToolStatus>(),
            PendingAction = pendingAction,
            ThreadStatus = string.IsNullOrWhiteSpace(threadStatus) ? ChatThreadStatuses.Active : threadStatus,
            Usage = usage
        };

    /// <summary>
    /// Creates a successful chat turn result for a stale or unknown thread that was replaced by a new thread.
    /// </summary>
    public static ChatTurnResult ThreadReplaced(
        string message,
        string newThreadId,
        DateTimeOffset? latestMessageDate = null,
        IReadOnlyList<ToolStatus>? toolStatuses = null,
        ChatActionSummary? pendingAction = null,
        ChatUsageInfo? usage = null) =>
        Success(message, newThreadId, latestMessageDate, toolStatuses, pendingAction, ChatThreadStatuses.ThreadReplaced, usage);

    /// <summary>Creates a failed chat turn result carrying a stable error code and a user-safe message.</summary>
    public static ChatTurnResult Failure(
        string errorCode,
        string userSafeMessage,
        string threadId,
        IReadOnlyList<ToolStatus>? toolStatuses = null) =>
        new()
        {
            Message = userSafeMessage,
            ThreadId = threadId ?? string.Empty,
            ErrorCode = string.IsNullOrWhiteSpace(errorCode) ? ChatErrorCodes.InternalError : errorCode,
            ToolStatuses = toolStatuses ?? Array.Empty<ToolStatus>()
        };
}

/// <summary>
/// User-facing diagnostic/usage information for a single chat turn: how long it took,
/// how many conversation turns have occurred, and how many tokens/AI Credits it consumed.
/// </summary>
public sealed record ChatUsageInfo
{
    /// <summary>Wall-clock duration of the turn, in seconds.</summary>
    public required double DurationSeconds { get; init; }

    /// <summary>Total number of turns completed so far in this conversation, including this one.</summary>
    public required int TurnCount { get; init; }

    /// <summary>Number of model calls the assistant made while producing this turn.</summary>
    public int ModelCallCount { get; init; }

    /// <summary>Sum of input tokens across all model calls in the turn.</summary>
    public long? InputTokens { get; init; }

    /// <summary>Sum of output tokens across all model calls in the turn.</summary>
    public long? OutputTokens { get; init; }

    /// <summary>Sum of cache-read tokens across all model calls in the turn.</summary>
    public long? CacheReadTokens { get; init; }

    /// <summary>Sum of cache-write tokens across all model calls in the turn.</summary>
    public long? CacheWriteTokens { get; init; }

    /// <summary>Sum of AI Credits cost across all model calls in the turn.</summary>
    public double? TotalCost { get; init; }
}
