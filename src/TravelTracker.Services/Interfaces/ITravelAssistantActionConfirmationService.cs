using TravelTracker.Services.Models;

namespace TravelTracker.Services.Interfaces;

/// <summary>
/// Provider-neutral interface for confirming or cancelling travel assistant actions.
/// Operates over opaque action IDs returned by <see cref="ITravelAssistantActionService"/>.
/// All public methods receive a trusted <see cref="TravelAssistantUserContext"/> derived from
/// the authenticated principal; no cross-user, cross-thread, or expired actions are permitted.
///
/// The implementation enforces one serializable SQL transaction: claim the pending action, verify
/// ownership/expiry/idempotency, recheck duplicates, insert the location, and record completion.
/// Rollback leaves the action pending; retry returns the prior result.
/// </summary>
public interface ITravelAssistantActionConfirmationService
{
    /// <summary>
    /// Confirm a prepared location-add action and persist it to the database.
    /// The action must be pending, owned by the authenticated user, not expired, and not previously
    /// confirmed. The confirmation is idempotent: retry with the same action ID returns the prior
    /// location ID.
    ///
    /// The implementation uses a serializable transaction:
    /// 1. Claim and reload the pending row.
    /// 2. Verify ownership, expiry, and prior confirmation status.
    /// 3. Decrypt and validate the idempotency key.
    /// 4. Recheck duplicates (by idempotency key or coordinates).
    /// 5. Insert the location.
    /// 6. Record the action as confirmed with the location ID.
    ///
    /// If any step fails, the transaction rolls back and the action remains pending (or is
    /// recorded as failed).
    /// </summary>
    /// <param name="user">Trusted authenticated user context from the principal.</param>
    /// <param name="actionId">Opaque action ID from a prior <c>PrepareAddLocationAsync</c> call.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>
    /// A <see cref="ConfirmActionResult"/> with success flag, created location ID (if successful),
    /// action state, and error details if confirmation failed.
    /// </returns>
    Task<ConfirmActionResult> ConfirmActionAsync(
        TravelAssistantUserContext user,
        string threadId,
        string actionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel a prepared location-add action without persisting a location.
    /// The action must be pending and owned by the authenticated user.
    /// </summary>
    /// <param name="user">Trusted authenticated user context from the principal.</param>
    /// <param name="actionId">Opaque action ID from a prior <c>PrepareAddLocationAsync</c> call.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>
    /// A <see cref="CancelActionResult"/> with success flag and error details if cancellation failed.
    /// </returns>
    Task<CancelActionResult> CancelActionAsync(
        TravelAssistantUserContext user,
        string threadId,
        string actionId,
        CancellationToken cancellationToken = default);
}
