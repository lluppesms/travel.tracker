using System.Security.Claims;

using TravelTracker.Services.Models;

namespace TravelTracker.Services;

/// <summary>
/// Registered in place of <c>CurrentTravelUserResolver</c> when SQL action storage is not configured and
/// therefore <c>IUserService</c> does not exist. Always resolves to no user, so assistant entry points can
/// report a clear reason instead of failing to activate with a dependency injection error.
/// </summary>
public sealed class UnavailableTravelUserResolver : ICurrentTravelUserResolver
{
    public Task<TravelAssistantUserContext?> ResolveAsync(ClaimsPrincipal? principal, CancellationToken cancellationToken = default)
        => Task.FromResult<TravelAssistantUserContext?>(null);

    public Task<TravelAssistantUserContext?> ResolveCurrentAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<TravelAssistantUserContext?>(null);
}
