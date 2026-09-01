using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components.Authorization;

using TravelTracker.Services.Interfaces;

namespace TravelTracker.Services;

/// <summary>
/// Supplies the authenticated principal from the Blazor Server circuit's authentication state, which is the
/// only valid circuit identity source for interactive components.
/// </summary>
public class AuthenticationStatePrincipalAccessor(AuthenticationStateProvider authenticationStateProvider) : ICurrentPrincipalAccessor
{
    private readonly AuthenticationStateProvider _authenticationStateProvider = authenticationStateProvider;

    public async Task<ClaimsPrincipal?> GetCurrentPrincipalAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var authenticationState = await _authenticationStateProvider.GetAuthenticationStateAsync().ConfigureAwait(false);
        return authenticationState?.User;
    }
}
