namespace TravelTracker.Services.Models;

/// <summary>
/// Result of preparing a location-add action. Contains success/failure status, opaque action ID
/// (for UI confirmation/cancellation), canonical summary (for display), and error details if
/// preparation failed.
/// </summary>
public sealed record PrepareAddLocationResult
{
    /// <summary>
    /// Gets a value indicating whether the action was successfully prepared.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the opaque action ID for UI confirmation or cancellation.
    /// Only populated when <see cref="Success"/> is true.
    /// </summary>
    public string? ActionId { get; init; }

    /// <summary>
    /// Gets a sanitized, UI-safe summary of the prepared action.
    /// Excludes full addresses, comments, and sensitive fields.
    /// Example: "Add Buffalo House (RV Park) for 2026-08-31"
    /// </summary>
    public string? Summary { get; init; }

    /// <summary>
    /// Gets the canonical (resolved) ISO date for the visit.
    /// Reflects the server's deterministic interpretation of the user's date expression.
    /// </summary>
    public string? CanonicalIsoDate { get; init; }

    /// <summary>
    /// Gets the resolved location type name (normalized from input).
    /// </summary>
    public string? ResolvedLocationType { get; init; }

    /// <summary>
    /// Gets the error code if preparation failed (e.g., "InvalidLocationType", "DateResolutionFailed").
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// Gets a user-facing error message if preparation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets additional error details (e.g., list of valid location types if the input type was invalid).
    /// </summary>
    public object? ErrorDetails { get; init; }
}
