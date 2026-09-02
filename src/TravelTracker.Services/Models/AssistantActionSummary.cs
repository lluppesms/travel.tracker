namespace TravelTracker.Services.Models;

/// <summary>
/// Summary of a pending assistant action for display in the UI.
/// Contains the opaque action ID, canonical summary, and creation timestamp.
/// Excludes full addresses, comments, and other sensitive details.
/// </summary>
public sealed record AssistantActionSummary
{
    /// <summary>
    /// Gets the opaque action ID for confirmation or cancellation.
    /// </summary>
    public required string ActionId { get; init; }

    /// <summary>
    /// Gets a sanitized, UI-safe summary of the action.
    /// Example: "Add Buffalo House (RV Park) for 2026-08-31"
    /// </summary>
    public required string Summary { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the action was created.
    /// </summary>
    public required DateTime CreatedAtUtc { get; init; }

    /// <summary>
    /// Gets the current action state ("Pending", "Confirmed", "Cancelled", "Failed").
    /// </summary>
    public required string State { get; init; }

    public required DateTime ExpiresAtUtc { get; init; }
}
