using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using TravelTracker.Services.Interfaces;
using TravelTracker.Services.Models;

namespace TravelTracker.Services.Services;

/// <summary>
/// Resolves the travel assistant identity from a trusted authenticated principal only.
/// The <c>X-API-Key</c> global key path used by <c>AuthenticationService</c> is deliberately not consulted here,
/// so the global key can never select a user on the assistant surface (SEC-004).
/// </summary>
public class CurrentTravelUserResolver(
    IUserService userService,
    ICurrentPrincipalAccessor principalAccessor,
    ILogger<CurrentTravelUserResolver> logger) : ICurrentTravelUserResolver
{
    private readonly IUserService _userService = userService;
    private readonly ICurrentPrincipalAccessor _principalAccessor = principalAccessor;
    private readonly ILogger<CurrentTravelUserResolver> _logger = logger;

    public async Task<TravelAssistantUserContext?> ResolveAsync(ClaimsPrincipal? principal, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (principal?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var externalId = GetExternalId(principal);
        if (string.IsNullOrWhiteSpace(externalId))
        {
            _logger.LogWarning("Authenticated principal did not contain a usable external identifier claim.");
            return null;
        }

        var displayName = GetDisplayName(principal);
        var email = GetEmail(principal);

        var user = await _userService.GetOrCreateUserAsync(externalId, displayName, email).ConfigureAwait(false);
        if (user is null || user.Id <= 0)
        {
            _logger.LogWarning("No internal user record could be resolved for the authenticated principal.");
            return null;
        }

        return new TravelAssistantUserContext(user.Id, externalId, displayName, email);
    }

    public async Task<TravelAssistantUserContext?> ResolveCurrentAsync(CancellationToken cancellationToken = default)
    {
        var principal = await _principalAccessor.GetCurrentPrincipalAsync(cancellationToken).ConfigureAwait(false);
        return await ResolveAsync(principal, cancellationToken).ConfigureAwait(false);
    }

    private static string GetExternalId(ClaimsPrincipal principal)
        => principal.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
            ?? principal.FindFirst("oid")?.Value
            ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? string.Empty;

    private static string GetDisplayName(ClaimsPrincipal principal)
        => principal.FindFirst("name")?.Value
            ?? principal.FindFirst(ClaimTypes.Name)?.Value
            ?? principal.Identity?.Name
            ?? "Unknown User";

    private static string GetEmail(ClaimsPrincipal principal)
        => principal.FindFirst("email")?.Value
            ?? principal.FindFirst(ClaimTypes.Email)?.Value
            ?? principal.FindFirst("preferred_username")?.Value
            ?? string.Empty;
}
