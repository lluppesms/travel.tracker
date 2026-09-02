namespace TravelTracker.Services.Models;

/// <summary>
/// Result of confirming a location-add action. Contains success/failure status, created location ID,
/// action state, and error details if confirmation failed.
/// </summary>
public sealed record ConfirmActionResult
{
    /// <summary>
    /// Gets a value indicating whether the action was successfully confirmed and the location was created.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the ID of the created location. Only populated when <see cref="Success"/> is true and
    /// the location was successfully inserted.
    /// </summary>
    public int? CreatedLocationId { get; init; }

    /// <summary>
    /// Gets the current state of the action ("Pending", "Confirmed", "Cancelled", "Failed").
    /// </summary>
    public string? ActionState { get; init; }

    /// <summary>
    /// Gets the error code if confirmation failed (e.g., "ActionNotFound", "ActionExpired", "ActionAlreadyConfirmed").
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// Gets a user-facing error message if confirmation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets a sanitized summary of the action state (for UI display after confirmation).
    /// Example: "Location added: Buffalo House"
    /// </summary>
    public string? Summary { get; init; }
}
