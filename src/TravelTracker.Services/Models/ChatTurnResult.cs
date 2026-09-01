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
        string? threadStatus = null) =>
        new()
        {
            Message = message,
            ThreadId = threadId,
            LatestMessageDate = latestMessageDate,
            ToolStatuses = toolStatuses ?? Array.Empty<ToolStatus>(),
            PendingAction = pendingAction,
            ThreadStatus = string.IsNullOrWhiteSpace(threadStatus) ? ChatThreadStatuses.Active : threadStatus
        };

    /// <summary>
    /// Creates a successful chat turn result for a stale or unknown thread that was replaced by a new thread.
    /// </summary>
    public static ChatTurnResult ThreadReplaced(
        string message,
        string newThreadId,
        DateTimeOffset? latestMessageDate = null,
        IReadOnlyList<ToolStatus>? toolStatuses = null,
        ChatActionSummary? pendingAction = null) =>
        Success(message, newThreadId, latestMessageDate, toolStatuses, pendingAction, ChatThreadStatuses.ThreadReplaced);

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
