namespace TravelTracker.Services.Models;

/// <summary>
/// Result of cancelling a location-add action. Contains success/failure status and error details
/// if cancellation failed.
/// </summary>
public sealed record CancelActionResult
{
    /// <summary>
    /// Gets a value indicating whether the action was successfully cancelled.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the error code if cancellation failed (e.g., "ActionNotFound", "ActionNotPending").
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// Gets a user-facing error message if cancellation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    public string? ActionState { get; init; }

    public string? Summary { get; init; }
}
