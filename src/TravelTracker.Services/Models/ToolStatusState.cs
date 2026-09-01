namespace TravelTracker.Services.Models;

/// <summary>
/// Lifecycle state of a single assistant tool call as displayed to the user.
/// </summary>
public enum ToolStatusState
{
    /// <summary>The tool call has started and has not completed yet.</summary>
    Started = 0,

    /// <summary>The tool call completed successfully.</summary>
    Succeeded = 1,

    /// <summary>The tool call failed.</summary>
    Failed = 2
}
