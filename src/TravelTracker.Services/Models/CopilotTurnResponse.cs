namespace TravelTracker.Services.Models;

/// <summary>
/// Aggregated result of a single non-streaming Copilot turn, including token and cost usage
/// summed across every model call the SDK made while producing the final response.
/// </summary>
public sealed record CopilotTurnResponse
{
    /// <summary>Final assistant message text, or <see langword="null"/> when no response was produced.</summary>
    public string? Content { get; init; }

    /// <summary>Number of model calls (usage events) observed while producing this turn.</summary>
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
