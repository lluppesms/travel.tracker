using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

using TravelTracker.Data.Configuration;
using TravelTracker.Data.Models;
using TravelTracker.Data.Repositories;
using TravelTracker.Services.Models;

namespace TravelTracker.Services.Services;

public sealed class TravelAssistantActionConfirmationService : ITravelAssistantActionConfirmationService
{
    private static readonly JsonSerializerOptions CanonicalJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IAssistantActionRepository _actionRepository;
    private readonly ILocationService _locationService;
    private readonly IDataProtector _protector;
    private readonly TimeProvider _timeProvider;
    private readonly TravelAssistantOptions _options;
    private readonly ILogger<TravelAssistantActionConfirmationService> _logger;

    public TravelAssistantActionConfirmationService(
        IAssistantActionRepository actionRepository,
        ILocationService locationService,
        IDataProtectionProvider dataProtectionProvider,
        TimeProvider timeProvider,
        IOptions<TravelAssistantOptions> options,
        ILogger<TravelAssistantActionConfirmationService> logger)
    {
        _actionRepository = actionRepository;
        _locationService = locationService;
        _protector = dataProtectionProvider.CreateProtector("TravelTracker.AssistantActions.CanonicalCommand.v1");
        _timeProvider = timeProvider;
        _options = options.Value;
        _logger = logger;
    }

    public Task<ConfirmActionResult> ConfirmActionAsync(
        TravelAssistantUserContext user,
        string actionId,
        CancellationToken cancellationToken = default) =>
        ConfirmActionCoreAsync(user, null, actionId, cancellationToken);

    public Task<ConfirmActionResult> ConfirmActionAsync(
        TravelAssistantUserContext user,
        string threadId,
        string actionId,
        CancellationToken cancellationToken = default) =>
        ConfirmActionCoreAsync(user, threadId, actionId, cancellationToken);

    private async Task<ConfirmActionResult> ConfirmActionCoreAsync(
        TravelAssistantUserContext user,
        string? expectedThreadId,
        string actionId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        if (!TryParseActionId(actionId, out var id)
            || (expectedThreadId is not null && string.IsNullOrWhiteSpace(expectedThreadId)))
        {
            return ConfirmFailure("action_not_found", "The action was not found.");
        }

        await using var transaction = await _actionRepository
            .BeginSerializableTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            var action = await _actionRepository.GetForUpdateAsync(id, cancellationToken).ConfigureAwait(false);
            var accessFailure = ValidateAccess(action, user, expectedThreadId);
            if (accessFailure is not null)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return accessFailure;
            }

            if (action!.State == AssistantActionStates.Confirmed)
            {
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return ConfirmSuccess(action);
            }

            if (action.State is AssistantActionStates.Cancelled or AssistantActionStates.Failed)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return ConfirmFailure(
                    "action_not_pending",
                    "The action is no longer pending.",
                    action.State,
                    action.SanitizedSummary);
            }

            var now = _timeProvider.GetUtcNow().UtcDateTime;
            if (action.ExpiresAt <= now || action.State == AssistantActionStates.Expired)
            {
                Expire(action, now);
                await _actionRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return ConfirmFailure(
                    "action_expired",
                    "The action expired. Prepare it again.",
                    AssistantActionStates.Expired,
                    action.SanitizedSummary);
            }

            if (action.State != AssistantActionStates.Pending
                || action.CommandSchemaVersion != TravelAssistantActionService.CommandSchemaVersion
                || action.ActionType != TravelAssistantActionService.ActionType
                || string.IsNullOrWhiteSpace(action.CanonicalCommandCiphertext))
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return ConfirmFailure("action_invalid", "The pending action cannot be executed.");
            }

            var canonicalJson = _protector.Unprotect(action.CanonicalCommandCiphertext);
            var actualHash = SHA256.HashData(Encoding.UTF8.GetBytes(canonicalJson));
            if (!CryptographicOperations.FixedTimeEquals(actualHash, action.PayloadHashSha256))
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return ConfirmFailure("action_payload_mismatch", "The pending action failed integrity validation.");
            }

            var command = JsonSerializer.Deserialize<AssistantActionCommand>(canonicalJson, CanonicalJsonOptions);
            if (command is null
                || !DateOnly.TryParseExact(
                    command.VisitDate,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var visitDate))
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return ConfirmFailure("action_payload_invalid", "The pending action payload is invalid.");
            }

            var duplicate = await _locationService.FindDuplicateAsync(
                user.UserId,
                command.LocationName,
                visitDate,
                command.City,
                command.State,
                cancellationToken).ConfigureAwait(false);
            if (duplicate is not null)
            {
                Fail(action, now, "duplicate_location");
                await _actionRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return ConfirmFailure(
                    "duplicate_location",
                    "This location is already recorded for that date.",
                    AssistantActionStates.Failed,
                    action.SanitizedSummary);
            }

            action.State = AssistantActionStates.Executing;
            action.ModifiedDate = now;
            await _actionRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            var location = await _locationService.CreateLocationAsync(
                new Location
                {
                    UserId = user.UserId,
                    Name = command.LocationName,
                    LocationTypeId = command.LocationTypeId,
                    LocationType = command.LocationTypeName,
                    Address = command.Address,
                    City = command.City,
                    State = command.State,
                    ZipCode = command.PostalCode,
                    Latitude = command.Latitude,
                    Longitude = command.Longitude,
                    StartDate = visitDate.ToDateTime(TimeOnly.MinValue),
                    EndDate = visitDate.ToDateTime(TimeOnly.MinValue),
                    Rating = command.Rating,
                    Comments = command.Comments,
                    AssistantActionId = action.Id
                },
                cancellationToken).ConfigureAwait(false);

            action.State = AssistantActionStates.Confirmed;
            action.CreatedLocationId = location.Id;
            action.CanonicalCommandCiphertext = null;
            action.ErrorCode = null;
            action.CompletedDate = now;
            action.ModifiedDate = now;
            action.RetainUntilDate = now.AddDays(Math.Max(1, _options.ActionAuditRetentionDays));
            await _actionRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return ConfirmSuccess(action);
        }
        catch (OperationCanceledException)
        {
            await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            _actionRepository.ClearTracking();
            throw;
        }
        catch (DbUpdateException exception)
        {
            await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            _actionRepository.ClearTracking();
            _logger.LogError(exception, "The assistant action transaction failed for action {ActionId}.", id);
            return ConfirmFailure("persistence_failed", "The action could not be saved. It remains pending.");
        }
        catch (CryptographicException exception)
        {
            await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            _actionRepository.ClearTracking();
            _logger.LogError(exception, "Assistant action decryption failed for action {ActionId}.", id);
            return ConfirmFailure("action_payload_unavailable", "The pending action cannot be decrypted.");
        }
        catch (JsonException exception)
        {
            await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            _actionRepository.ClearTracking();
            _logger.LogError(exception, "Assistant action JSON was invalid for action {ActionId}.", id);
            return ConfirmFailure("action_payload_invalid", "The pending action payload is invalid.");
        }
        catch (ArgumentException exception)
        {
            await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            _actionRepository.ClearTracking();
            _logger.LogWarning(exception, "Assistant action validation failed for action {ActionId}.", id);
            return ConfirmFailure("action_validation_failed", "The pending action is no longer valid.");
        }
        catch (InvalidOperationException exception)
        {
            await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            _actionRepository.ClearTracking();
            _logger.LogError(exception, "Assistant action execution failed for action {ActionId}.", id);
            return ConfirmFailure("persistence_failed", "The action could not be saved. It remains pending.");
        }
    }

    public Task<CancelActionResult> CancelActionAsync(
        TravelAssistantUserContext user,
        string actionId,
        CancellationToken cancellationToken = default) =>
        CancelActionCoreAsync(user, null, actionId, cancellationToken);

    public Task<CancelActionResult> CancelActionAsync(
        TravelAssistantUserContext user,
        string threadId,
        string actionId,
        CancellationToken cancellationToken = default) =>
        CancelActionCoreAsync(user, threadId, actionId, cancellationToken);

    private async Task<CancelActionResult> CancelActionCoreAsync(
        TravelAssistantUserContext user,
        string? expectedThreadId,
        string actionId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        if (!TryParseActionId(actionId, out var id)
            || (expectedThreadId is not null && string.IsNullOrWhiteSpace(expectedThreadId)))
        {
            return CancelFailure("action_not_found", "The action was not found.");
        }

        await using var transaction = await _actionRepository
            .BeginSerializableTransactionAsync(cancellationToken)
            .ConfigureAwait(false);
        try
        {
            var action = await _actionRepository.GetForUpdateAsync(id, cancellationToken).ConfigureAwait(false);
            if (action is null)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return CancelFailure("action_not_found", "The action was not found.");
            }

            if (action.UserId != user.UserId)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return CancelFailure("action_forbidden", "The action belongs to another user.");
            }

            if (expectedThreadId is not null
                && !string.Equals(action.ThreadId, expectedThreadId, StringComparison.Ordinal))
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return CancelFailure("action_thread_mismatch", "The action belongs to another thread.");
            }

            if (action.State == AssistantActionStates.Cancelled)
            {
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return new CancelActionResult
                {
                    Success = true,
                    ActionState = action.State,
                    Summary = action.SanitizedSummary
                };
            }

            if (action.State == AssistantActionStates.Confirmed)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return CancelFailure(
                    "action_already_confirmed",
                    "The action was already confirmed.",
                    action.State,
                    action.SanitizedSummary);
            }

            var now = _timeProvider.GetUtcNow().UtcDateTime;
            if (action.ExpiresAt <= now || action.State == AssistantActionStates.Expired)
            {
                Expire(action, now);
                await _actionRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return CancelFailure(
                    "action_expired",
                    "The action already expired.",
                    action.State,
                    action.SanitizedSummary);
            }

            if (action.State != AssistantActionStates.Pending)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return CancelFailure(
                    "action_not_pending",
                    "The action is not pending.",
                    action.State,
                    action.SanitizedSummary);
            }

            action.State = AssistantActionStates.Cancelled;
            action.CanonicalCommandCiphertext = null;
            action.ErrorCode = null;
            action.CompletedDate = now;
            action.ModifiedDate = now;
            action.RetainUntilDate = now.AddDays(Math.Max(1, _options.ActionAuditRetentionDays));
            await _actionRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return new CancelActionResult
            {
                Success = true,
                ActionState = action.State,
                Summary = action.SanitizedSummary
            };
        }
        catch (OperationCanceledException)
        {
            await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            _actionRepository.ClearTracking();
            throw;
        }
        catch (DbUpdateException exception)
        {
            await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            _actionRepository.ClearTracking();
            _logger.LogError(exception, "Cancelling assistant action {ActionId} failed.", id);
            return CancelFailure("persistence_failed", "The action could not be cancelled.");
        }
    }

    private ConfirmActionResult? ValidateAccess(
        AssistantAction? action,
        TravelAssistantUserContext user,
        string? expectedThreadId)
    {
        if (action is null)
        {
            return ConfirmFailure("action_not_found", "The action was not found.");
        }

        if (action.UserId != user.UserId)
        {
            return ConfirmFailure("action_forbidden", "The action belongs to another user.");
        }

        return expectedThreadId is not null
            && !string.Equals(action.ThreadId, expectedThreadId, StringComparison.Ordinal)
            ? ConfirmFailure("action_thread_mismatch", "The action belongs to another thread.")
            : null;
    }

    private void Expire(AssistantAction action, DateTime now)
    {
        action.State = AssistantActionStates.Expired;
        action.CanonicalCommandCiphertext = null;
        action.ErrorCode = "action_expired";
        action.CompletedDate = now;
        action.ModifiedDate = now;
        action.RetainUntilDate = now.AddDays(Math.Max(1, _options.ActionAuditRetentionDays));
    }

    private void Fail(AssistantAction action, DateTime now, string errorCode)
    {
        action.State = AssistantActionStates.Failed;
        action.CanonicalCommandCiphertext = null;
        action.ErrorCode = errorCode;
        action.CompletedDate = now;
        action.ModifiedDate = now;
        action.RetainUntilDate = now.AddDays(Math.Max(1, _options.ActionAuditRetentionDays));
    }

    private static bool TryParseActionId(string? actionId, out Guid id) =>
        Guid.TryParseExact(actionId, "N", out id);

    private static ConfirmActionResult ConfirmSuccess(AssistantAction action) =>
        new()
        {
            Success = action.CreatedLocationId is > 0,
            CreatedLocationId = action.CreatedLocationId,
            ActionState = action.State,
            Summary = action.SanitizedSummary,
            ErrorCode = action.CreatedLocationId is > 0 ? null : "location_id_missing",
            ErrorMessage = action.CreatedLocationId is > 0
                ? null
                : "The action completed without a persisted location ID."
        };

    private static ConfirmActionResult ConfirmFailure(
        string code,
        string message,
        string? state = null,
        string? summary = null) =>
        new()
        {
            Success = false,
            ErrorCode = code,
            ErrorMessage = message,
            ActionState = state,
            Summary = summary
        };

    private static CancelActionResult CancelFailure(
        string code,
        string message,
        string? state = null,
        string? summary = null) =>
        new()
        {
            Success = false,
            ErrorCode = code,
            ErrorMessage = message,
            ActionState = state,
            Summary = summary
        };
}
