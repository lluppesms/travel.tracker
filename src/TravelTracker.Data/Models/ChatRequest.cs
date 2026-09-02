namespace TravelTracker.Data.Models;

public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
    public string? ThreadId { get; set; }
    public DateTimeOffset? LastMessageDate { get; set; }
}

public class ChatResponse
{
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string ThreadId { get; set; } = string.Empty;
    public DateTimeOffset? LatestMessageDate { get; set; }

    /// <summary>Optional user-safe per-tool statuses for the turn.</summary>
    public IReadOnlyList<ChatToolStatusDto>? ToolStatuses { get; set; }

    /// <summary>Optional pending action awaiting confirmation.</summary>
    public ChatPendingActionDto? PendingAction { get; set; }

    /// <summary>Optional stable error code; null when the turn succeeded.</summary>
    public string? ErrorCode { get; set; }

    /// <summary>Optional thread status, for example <c>active</c> or <c>thread_replaced</c>.</summary>
    public string? ThreadStatus { get; set; }

    /// <summary>Optional diagnostic/usage information for the turn (duration, tokens, cost).</summary>
    public ChatUsageDto? Usage { get; set; }
}

/// <summary>User-safe status of a single assistant tool call.</summary>
public class ChatToolStatusDto
{
    public string ToolName { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public long? DurationMs { get; set; }
}

/// <summary>UI-safe summary of a pending assistant action awaiting confirmation.</summary>
public class ChatPendingActionDto
{
    public string ActionId { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? DisplayName { get; set; }
    public string? LocationText { get; set; }
    public DateOnly? Date { get; set; }
    public string? TypeName { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}

/// <summary>User-facing diagnostic/usage information for a single chat turn.</summary>
public class ChatUsageDto
{
    /// <summary>Wall-clock duration of the turn, in seconds.</summary>
    public double DurationSeconds { get; set; }

    /// <summary>Total number of turns completed so far in this conversation, including this one.</summary>
    public int TurnCount { get; set; }

    /// <summary>Number of model calls the assistant made while producing this turn.</summary>
    public int ModelCallCount { get; set; }

    /// <summary>Sum of input tokens across all model calls in the turn.</summary>
    public long? InputTokens { get; set; }

    /// <summary>Sum of output tokens across all model calls in the turn.</summary>
    public long? OutputTokens { get; set; }

    /// <summary>Sum of cache-read tokens across all model calls in the turn.</summary>
    public long? CacheReadTokens { get; set; }

    /// <summary>Sum of cache-write tokens across all model calls in the turn.</summary>
    public long? CacheWriteTokens { get; set; }

    /// <summary>Sum of AI Credits cost across all model calls in the turn.</summary>
    public double? TotalCost { get; set; }
}
