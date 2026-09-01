using System;

namespace TravelTracker.Services.Models;

/// <summary>
/// UI-safe summary of a pending assistant action awaiting user confirmation.
/// Carries only an opaque action identifier and display text; it must never expose a travel user identifier,
/// encrypted command payloads, canonical command JSON, secrets, or connection strings (SEC-003, SEC-010).
/// </summary>
public sealed record ChatActionSummary
{
    /// <summary>Opaque server-issued identifier used to confirm or cancel the action.</summary>
    public required string ActionId { get; init; }

    /// <summary>Provider-neutral action kind, for example <c>create_location</c>.</summary>
    public required string ActionType { get; init; }

    /// <summary>Short human-readable title for the confirmation card.</summary>
    public required string Title { get; init; }

    /// <summary>Optional longer human-readable summary of what will happen when confirmed.</summary>
    public string? Summary { get; init; }

    /// <summary>Optional display name of the subject of the action.</summary>
    public string? DisplayName { get; init; }

    /// <summary>Optional human-readable location text, for example <c>Springfield, IL</c>.</summary>
    public string? LocationText { get; init; }

    /// <summary>Optional resolved date associated with the action.</summary>
    public DateOnly? Date { get; init; }

    /// <summary>Optional display name of the resolved item type.</summary>
    public string? TypeName { get; init; }

    /// <summary>Instant after which the pending action can no longer be confirmed.</summary>
    public required DateTimeOffset ExpiresAt { get; init; }
}
