using System.Collections.Generic;

namespace TravelTracker.Services.Models;

/// <summary>
/// Stable, provider-neutral chat error codes returned to clients in the <c>errorCode</c> field.
/// Values are wire-stable and must not be renamed once released.
/// </summary>
public static class ChatErrorCodes
{
    /// <summary>Caller is not authenticated. Maps to HTTP 401.</summary>
    public const string Unauthorized = "unauthorized";

    /// <summary>Caller is authenticated but not allowed to perform the operation. Maps to HTTP 403.</summary>
    public const string Forbidden = "forbidden";

    /// <summary>The requested chat thread does not exist for this user. Maps to HTTP 404.</summary>
    public const string ThreadNotFound = "thread_not_found";

    /// <summary>The requested pending action does not exist for this user. Maps to HTTP 404.</summary>
    public const string ActionNotFound = "action_not_found";

    /// <summary>The requested thread was stale and a replacement thread was issued. Maps to HTTP 409.</summary>
    public const string ThreadReplaced = "thread_replaced";

    /// <summary>The pending action was already confirmed, cancelled, or otherwise conflicts. Maps to HTTP 409.</summary>
    public const string ActionConflict = "action_conflict";

    /// <summary>The pending action expired and can no longer be confirmed. Maps to HTTP 410.</summary>
    public const string ActionExpired = "action_expired";

    /// <summary>The caller exceeded the configured request limits. Maps to HTTP 429.</summary>
    public const string RateLimited = "rate_limited";

    /// <summary>The assistant provider is unavailable or timed out. Maps to HTTP 503.</summary>
    public const string ProviderUnavailable = "provider_unavailable";

    /// <summary>The request payload was missing or invalid. Maps to HTTP 400.</summary>
    public const string InvalidRequest = "invalid_request";

    /// <summary>An unexpected server-side failure occurred. Maps to HTTP 500.</summary>
    public const string InternalError = "internal_error";

    /// <summary>Documented mapping from stable error code to HTTP status code.</summary>
    public static IReadOnlyDictionary<string, int> HttpStatusCodes { get; } = new Dictionary<string, int>(StringComparer.Ordinal)
    {
        [Unauthorized] = 401,
        [Forbidden] = 403,
        [ThreadNotFound] = 404,
        [ActionNotFound] = 404,
        [ThreadReplaced] = 409,
        [ActionConflict] = 409,
        [ActionExpired] = 410,
        [RateLimited] = 429,
        [ProviderUnavailable] = 503,
        [InvalidRequest] = 400,
        [InternalError] = 500
    };

    /// <summary>
    /// Resolves the HTTP status code for a stable error code.
    /// Returns 200 for a null or empty code and 500 for an unrecognized code.
    /// </summary>
    public static int ToHttpStatusCode(string? errorCode)
    {
        if (string.IsNullOrWhiteSpace(errorCode))
        {
            return 200;
        }

        return HttpStatusCodes.TryGetValue(errorCode, out var statusCode) ? statusCode : 500;
    }
}
