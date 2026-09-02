using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

using TravelTracker.Data.Models;
using TravelTracker.Services.Models;

namespace TravelTracker.Authentication;

/// <summary>
/// Default authentication scheme used when Azure AD is not configured. Without it a challenge from
/// <c>[Authorize]</c> throws, and the developer exception page would return the exception message,
/// stack trace, and request headers to an unauthenticated caller (SEC-010).
/// This handler authenticates nobody and answers challenges with a bare 401 carrying no exception detail.
/// </summary>
public sealed class UnconfiguredAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory loggerFactory,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, loggerFactory, encoder)
{
    /// <summary>Name of the scheme registered when Azure AD is not configured.</summary>
    public const string SchemeName = "AuthenticationNotConfigured";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        => Task.FromResult(AuthenticateResult.NoResult());

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        => WriteStatusAsync(StatusCodes.Status401Unauthorized, ChatErrorCodes.Unauthorized, "Authentication is required.");

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
        => WriteStatusAsync(StatusCodes.Status403Forbidden, ChatErrorCodes.Forbidden, "You are not allowed to perform this operation.");

    private async Task WriteStatusAsync(int statusCode, string errorCode, string userSafeMessage)
    {
        if (Response.HasStarted)
        {
            return;
        }

        Response.StatusCode = statusCode;

        if (!Request.Path.StartsWithSegments("/api"))
        {
            return;
        }

        Response.ContentType = "application/json";
        await Response.WriteAsJsonAsync(new ChatResponse
        {
            Message = userSafeMessage,
            Timestamp = DateTime.UtcNow,
            ThreadId = string.Empty,
            ErrorCode = errorCode
        });
    }
}
