using TravelTracker.Services.Models;

namespace TravelTracker.Services.Interfaces;

/// <summary>
/// Provider-neutral interface for travel assistant actions. All public methods receive a trusted
/// <see cref="TravelAssistantUserContext"/> derived from the authenticated principal.
/// No public method accepts a model/client-supplied user ID.
///
/// The service implements the action boundary: deterministic interpretation, durable pending
/// commands, and atomic confirmed writes.
/// </summary>
public interface ITravelAssistantActionService
{
    Task<IReadOnlyList<AssistantLocationSearchResult>> SearchUserLocationsAsync(
        TravelAssistantUserContext user,
        string query,
        int limit = 25,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the configured location types as compact model-visible records.
    /// </summary>
    /// <param name="user">Trusted authenticated user context.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The valid location type names and descriptions.</returns>
    Task<IReadOnlyList<AssistantLocationTypeResult>> GetLocationTypesAsync(
        TravelAssistantUserContext user,
        CancellationToken cancellationToken = default);

    Task<LocationTypeResolutionResult> ResolveLocationTypeAsync(
        TravelAssistantUserContext user,
        string locationTypeName,
        CancellationToken cancellationToken = default);

    Task<PlaceLookupResult> LookupPlaceAsync(
        TravelAssistantUserContext user,
        PlaceLookupRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Prepare a location-add action without confirming the write. The model proposes a candidate,
    /// location type, date, and optional address fields. The service returns a pending action ID,
    /// canonical summary, and success flag.
    ///
    /// The returned action ID is opaque to the model and used by the UI for confirmation or cancellation.
    /// If the preparation fails (invalid type, date resolution error, ambiguous candidates), the action
    /// is not created and an error is returned.
    /// </summary>
    /// <param name="user">Trusted authenticated user context from the principal.</param>
    /// <param name="candidateId">Opaque 15-minute candidate ID from prior <c>lookup_place</c> call.</param>
    /// <param name="locationName">The display name for the location.</param>
    /// <param name="locationTypeName">The location type (e.g., "RV Park"). Must match a configured type.</param>
    /// <param name="dateExpression">
    /// The user-supplied relative or ISO date expression (e.g., "Yesterday", "2026-08-31").
    /// The service resolves relative expressions deterministically using server time/timezone.
    /// </param>
    /// <param name="proposedIsoDate">
    /// Optional ISO date from the model (advisory only). If provided, must match the resolved
    /// server result or the preparation fails.
    /// </param>
    /// <param name="address">Optional street address. Stored as untrusted user-supplied text.</param>
    /// <param name="city">Optional city name.</param>
    /// <param name="state">Optional state/region code.</param>
    /// <param name="postalCode">Optional postal code.</param>
    /// <param name="latitude">Optional geographic latitude from the candidate or model.</param>
    /// <param name="longitude">Optional geographic longitude from the candidate or model.</param>
    /// <param name="comments">Optional user comments. Stored as untrusted text, excluded from model-visible results.</param>
    /// <param name="rating">Optional numeric rating (0-10). Must pass data-annotation validation.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>
    /// A <see cref="PrepareAddLocationResult"/> with a success flag, opaque action ID (if successful),
    /// canonical summary, and error details if preparation failed.
    /// </returns>
    Task<PrepareAddLocationResult> PrepareAddLocationAsync(
        TravelAssistantUserContext user,
        string threadId,
        string candidateId,
        string locationName,
        string locationTypeName,
        string dateExpression,
        string? proposedIsoDate = null,
        string? address = null,
        string? city = null,
        string? state = null,
        string? postalCode = null,
        double? latitude = null,
        double? longitude = null,
        string? comments = null,
        int? rating = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieve unexpired pending actions for the authenticated user. Used by the UI to recover
    /// confirmation cards after refresh or reconnect.
    /// </summary>
    /// <param name="user">Trusted authenticated user context from the principal.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A collection of pending action summaries, sorted by creation time descending.</returns>
    Task<IReadOnlyList<AssistantActionSummary>> GetPendingActionsAsync(
        TravelAssistantUserContext user,
        string? threadId = null,
        CancellationToken cancellationToken = default);
}
