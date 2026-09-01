namespace TravelTracker.Services.Models;

/// <summary>
/// User-safe status of a single assistant tool call.
/// Must never carry prompt text, tool payloads, tokens, comments, addresses, or model reasoning (SEC-003, SEC-010).
/// </summary>
public sealed record ToolStatus
{
    /// <summary>Display name of the tool that was invoked, for example <c>search_locations</c>.</summary>
    public required string ToolName { get; init; }

    /// <summary>Lifecycle state of the tool call.</summary>
    public required ToolStatusState State { get; init; }

    /// <summary>Optional short, user-safe description of the outcome.</summary>
    public string? Detail { get; init; }

    /// <summary>Optional elapsed time of the tool call in milliseconds.</summary>
    public long? DurationMs { get; init; }

    /// <summary>Creates a <see cref="ToolStatusState.Started"/> status.</summary>
    public static ToolStatus Started(string toolName, string? detail = null) =>
        new() { ToolName = toolName, State = ToolStatusState.Started, Detail = detail };

    /// <summary>Creates a <see cref="ToolStatusState.Succeeded"/> status.</summary>
    public static ToolStatus Succeeded(string toolName, string? detail = null, long? durationMs = null) =>
        new() { ToolName = toolName, State = ToolStatusState.Succeeded, Detail = detail, DurationMs = durationMs };

    /// <summary>Creates a <see cref="ToolStatusState.Failed"/> status.</summary>
    public static ToolStatus Failed(string toolName, string? detail = null, long? durationMs = null) =>
        new() { ToolName = toolName, State = ToolStatusState.Failed, Detail = detail, DurationMs = durationMs };
}
