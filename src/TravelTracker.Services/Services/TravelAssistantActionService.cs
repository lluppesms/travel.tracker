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

public sealed class TravelAssistantActionService : ITravelAssistantActionService
{
    internal const int CommandSchemaVersion = 1;
    internal const string ActionType = "create_location";

    private static readonly JsonSerializerOptions CanonicalJsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private readonly ILocationLookupService _locationLookupService;
    private readonly ILocationService _locationService;
    private readonly ILocationTypeService _locationTypeService;
    private readonly IRelativeDateResolver _relativeDateResolver;
    private readonly IAssistantActionRepository _actionRepository;
    private readonly IDataProtector _protector;
    private readonly TimeProvider _timeProvider;
    private readonly TravelAssistantOptions _options;
    private readonly ILogger<TravelAssistantActionService> _logger;

    public TravelAssistantActionService(
        ILocationLookupService locationLookupService,
        ILocationService locationService,
        ILocationTypeService locationTypeService,
        IRelativeDateResolver relativeDateResolver,
        IAssistantActionRepository actionRepository,
        IDataProtectionProvider dataProtectionProvider,
        TimeProvider timeProvider,
        IOptions<TravelAssistantOptions> options,
        ILogger<TravelAssistantActionService> logger)
    {
        _locationLookupService = locationLookupService;
        _locationService = locationService;
        _locationTypeService = locationTypeService;
        _relativeDateResolver = relativeDateResolver;
        _actionRepository = actionRepository;
        _protector = dataProtectionProvider.CreateProtector("TravelTracker.AssistantActions.CanonicalCommand.v1");
        _timeProvider = timeProvider;
        _options = options.Value;
        _logger = logger;
    }

    public Task<IReadOnlyList<AssistantLocationSearchResult>> SearchUserLocationsAsync(
        TravelAssistantUserContext user,
        string query,
        int limit = 25,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        var boundedLimit = Math.Clamp(
            limit,
            1,
            Math.Clamp(_options.MaxLocationSearchResults, 1, 25));
        return _locationService.SearchForAssistantAsync(user.UserId, query, boundedLimit, cancellationToken);
    }

    public async Task<IReadOnlyList<AssistantLocationTypeResult>> GetLocationTypesAsync(
        TravelAssistantUserContext user,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        cancellationToken.ThrowIfCancellationRequested();

        var locationTypes = await _locationTypeService.GetAllLocationTypesAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        return locationTypes
            .OrderBy(locationType => locationType.Name, StringComparer.OrdinalIgnoreCase)
            .Take(100)
            .Select(locationType => new AssistantLocationTypeResult
            {
                Name = locationType.Name,
                Description = locationType.Description
            })
            .ToArray();
    }

    public Task<LocationTypeResolutionResult> ResolveLocationTypeAsync(
        TravelAssistantUserContext user,
        string locationTypeName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        return _locationTypeService.ResolveLocationTypeAsync(locationTypeName, cancellationToken);
    }

    public Task<PlaceLookupResult> LookupPlaceAsync(
        TravelAssistantUserContext user,
        PlaceLookupRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        return _locationLookupService.LookupPlaceAsync(request, cancellationToken);
    }

    public async Task<PrepareAddLocationResult> PrepareAddLocationAsync(
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
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(threadId) || threadId.Length > 200)
        {
            return Failure("invalid_thread", "The assistant thread is invalid.");
        }

        if (string.IsNullOrWhiteSpace(candidateId))
        {
            return Failure("candidate_required", "Choose a place candidate before preparing the location.");
        }

        var candidate = await _locationLookupService.ResolveCandidateAsync(candidateId, cancellationToken)
            .ConfigureAwait(false);
        if (candidate is null)
        {
            return Failure("candidate_expired", "The place candidate is invalid or expired. Look up the place again.");
        }

        if (candidate.CoordinateDivergenceDetected)
        {
            return Failure("candidate_ambiguous", "The place providers disagree about the coordinates. Clarify the place.");
        }

        var typeResolution = await _locationTypeService.ResolveLocationTypeAsync(locationTypeName, cancellationToken)
            .ConfigureAwait(false);
        if (typeResolution.Status != LocationTypeResolutionStatus.Found || typeResolution.LocationType is null)
        {
            return new PrepareAddLocationResult
            {
                Success = false,
                ErrorCode = typeResolution.Status == LocationTypeResolutionStatus.Ambiguous
                    ? "location_type_ambiguous"
                    : "location_type_invalid",
                ErrorMessage = typeResolution.Status == LocationTypeResolutionStatus.Ambiguous
                    ? "The location type is ambiguous. Choose one valid type."
                    : "The location type is not recognized.",
                ErrorDetails = new { validTypes = typeResolution.Matches }
            };
        }

        var dateResult = ResolveVisitDate(dateExpression, proposedIsoDate);
        if (!dateResult.Success)
        {
            return Failure(dateResult.ErrorCode!, dateResult.ErrorMessage!);
        }

        var visitDate = dateResult.Date!.Value;
        var canonicalName = NormalizeRequired(
            string.IsNullOrWhiteSpace(locationName) ? candidate.Name : locationName,
            200);
        if (canonicalName is null)
        {
            return Failure("location_name_invalid", "The location name is required and must be 200 characters or fewer.");
        }

        var canonicalCity = NormalizeOptional(candidate.City, city, 100);
        var canonicalState = NormalizeOptional(candidate.State, state, 50).ToUpperInvariant();
        var canonicalAddress = NormalizeOptional(candidate.Address, address, 300);
        var canonicalPostalCode = NormalizeOptional(candidate.PostalCode, postalCode, 20);
        var canonicalComments = NormalizeOptional(null, comments, 2000);
        var canonicalRating = rating ?? 0;

        if (canonicalRating is < 0 or > 5)
        {
            return Failure("rating_invalid", "Rating must be between 0 and 5.");
        }

        var canonicalLatitude = Math.Round(candidate.Latitude, 6);
        var canonicalLongitude = Math.Round(candidate.Longitude, 6);
        if (canonicalLatitude is < -90 or > 90 || canonicalLongitude is < -180 or > 180)
        {
            return Failure("coordinates_invalid", "The selected candidate has invalid coordinates.");
        }

        if (latitude is not null && Math.Abs(latitude.Value - canonicalLatitude) > 0.000001
            || longitude is not null && Math.Abs(longitude.Value - canonicalLongitude) > 0.000001)
        {
            return Failure("candidate_payload_mismatch", "The proposed coordinates do not match the selected candidate.");
        }

        var duplicate = await _locationService.FindDuplicateAsync(
            user.UserId,
            canonicalName,
            visitDate,
            canonicalCity,
            canonicalState,
            cancellationToken).ConfigureAwait(false);
        if (duplicate is not null)
        {
            return new PrepareAddLocationResult
            {
                Success = false,
                ErrorCode = "duplicate_location",
                ErrorMessage = "This location is already recorded for that date.",
                ErrorDetails = new { existingLocationId = duplicate.Id }
            };
        }

        var command = new AssistantActionCommand
        {
            LocationName = canonicalName,
            LocationTypeId = typeResolution.LocationType.Id,
            LocationTypeName = typeResolution.LocationType.Name,
            VisitDate = visitDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            Address = canonicalAddress,
            City = canonicalCity,
            State = canonicalState,
            PostalCode = canonicalPostalCode,
            Latitude = canonicalLatitude,
            Longitude = canonicalLongitude,
            Comments = canonicalComments,
            Rating = canonicalRating
        };

        var canonicalJson = JsonSerializer.Serialize(command, CanonicalJsonOptions);
        var payloadHash = SHA256.HashData(Encoding.UTF8.GetBytes(canonicalJson));
        var idempotencyKey = Convert.ToHexString(payloadHash).ToLowerInvariant();
        var existing = await _actionRepository.GetByIdempotencyKeyAsync(
            user.UserId,
            threadId,
            idempotencyKey,
            cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            return ExistingActionResult(existing, payloadHash);
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var summary = $"Add {canonicalName} ({typeResolution.LocationType.Name}) for {command.VisitDate}";
        var action = new AssistantAction
        {
            Id = Guid.NewGuid(),
            UserId = user.UserId,
            ThreadId = threadId,
            ActionType = ActionType,
            CommandSchemaVersion = CommandSchemaVersion,
            State = AssistantActionStates.Pending,
            CanonicalIdempotencyKey = idempotencyKey,
            CanonicalCommandCiphertext = _protector.Protect(canonicalJson),
            PayloadHashSha256 = payloadHash,
            SanitizedSummary = summary,
            CreatedDate = now,
            ModifiedDate = now,
            ExpiresAt = now.AddHours(Math.Max(1, _options.PendingActionExpiryHours)),
            RetainUntilDate = now.AddDays(Math.Max(1, _options.ActionAuditRetentionDays))
        };

        try
        {
            await _actionRepository.AddAsync(action, cancellationToken).ConfigureAwait(false);
            await _actionRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException exception)
        {
            _logger.LogInformation(exception, "An equivalent assistant action was prepared concurrently.");
            _actionRepository.Detach(action);
            existing = await _actionRepository.GetByIdempotencyKeyAsync(
                user.UserId,
                threadId,
                idempotencyKey,
                cancellationToken).ConfigureAwait(false);
            if (existing is null)
            {
                throw;
            }

            return ExistingActionResult(existing, payloadHash);
        }

        return new PrepareAddLocationResult
        {
            Success = true,
            ActionId = action.Id.ToString("N"),
            Summary = summary,
            CanonicalIsoDate = command.VisitDate,
            ResolvedLocationType = command.LocationTypeName
        };
    }

    public async Task<IReadOnlyList<AssistantActionSummary>> GetPendingActionsAsync(
        TravelAssistantUserContext user,
        string? threadId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        var actions = await _actionRepository.GetPendingAsync(
            user.UserId,
            _timeProvider.GetUtcNow().UtcDateTime,
            cancellationToken).ConfigureAwait(false);

        return actions
            .Where(action => string.IsNullOrWhiteSpace(threadId)
                || string.Equals(action.ThreadId, threadId, StringComparison.Ordinal))
            .Select(action => new AssistantActionSummary
            {
                ActionId = action.Id.ToString("N"),
                Summary = action.SanitizedSummary,
                CreatedAtUtc = action.CreatedDate,
                ExpiresAtUtc = action.ExpiresAt,
                State = action.State
            })
            .ToArray();
    }

    private DateResult ResolveVisitDate(string expression, string? proposedIsoDate)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return DateResult.Failed("date_required", "A visit date is required.");
        }

        DateOnly? proposedDate = null;
        if (!string.IsNullOrWhiteSpace(proposedIsoDate))
        {
            if (!DateOnly.TryParseExact(
                    proposedIsoDate,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsedProposedDate))
            {
                return DateResult.Failed("date_invalid", "The proposed date must use YYYY-MM-DD.");
            }

            proposedDate = parsedProposedDate;
        }

        if (DateOnly.TryParseExact(
                expression.Trim(),
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var absoluteDate))
        {
            return proposedDate is not null && proposedDate != absoluteDate
                ? DateResult.Failed("date_mismatch", "The proposed date does not match the requested date.")
                : DateResult.Resolved(absoluteDate);
        }

        var resolution = _relativeDateResolver.Resolve(expression, proposedDate);
        return resolution.Status switch
        {
            RelativeDateResolutionStatus.Resolved => DateResult.Resolved(resolution.Date!.Value),
            RelativeDateResolutionStatus.DateDisagrees =>
                DateResult.Failed("date_mismatch", resolution.Message ?? "The proposed date does not match."),
            _ => DateResult.Failed("date_clarification_required", resolution.Message ?? "Clarify the visit date.")
        };
    }

    private PrepareAddLocationResult ExistingActionResult(AssistantAction action, byte[] payloadHash)
    {
        if (!CryptographicOperations.FixedTimeEquals(action.PayloadHashSha256, payloadHash))
        {
            return Failure("action_payload_conflict", "An action with the same idempotency key has different content.");
        }

        if (action.State is AssistantActionStates.Cancelled or AssistantActionStates.Expired or AssistantActionStates.Failed)
        {
            return Failure("action_not_pending", "An equivalent action already reached a terminal state.");
        }

        return new PrepareAddLocationResult
        {
            Success = true,
            ActionId = action.Id.ToString("N"),
            Summary = action.SanitizedSummary,
            CanonicalIsoDate = null,
            ResolvedLocationType = null
        };
    }

    private static PrepareAddLocationResult Failure(string code, string message) =>
        new() { Success = false, ErrorCode = code, ErrorMessage = message };

    private static string? NormalizeRequired(string? value, int maxLength)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) || normalized.Length > maxLength ? null : normalized;
    }

    private static string NormalizeOptional(string? authoritative, string? fallback, int maxLength)
    {
        var value = !string.IsNullOrWhiteSpace(authoritative) ? authoritative : fallback;
        value = value?.Trim() ?? string.Empty;
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private sealed record DateResult(bool Success, DateOnly? Date, string? ErrorCode, string? ErrorMessage)
    {
        public static DateResult Resolved(DateOnly date) => new(true, date, null, null);
        public static DateResult Failed(string code, string message) => new(false, null, code, message);
    }
}
