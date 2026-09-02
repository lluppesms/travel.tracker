using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using TravelTracker.Services.Interfaces;

namespace TravelTracker.Services;

/// <summary>
/// Default accessor for a Blazor Server application that also hosts controllers. When the scope belongs to an
/// HTTP request the request principal is used; otherwise the Blazor circuit authentication state is used.
/// Register this as the single scoped <see cref="ICurrentPrincipalAccessor"/> implementation.
/// </summary>
public class CurrentPrincipalAccessor(
    IHttpContextAccessor httpContextAccessor,
    AuthenticationStateProvider? authenticationStateProvider,
    ILogger<CurrentPrincipalAccessor> logger) : ICurrentPrincipalAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly AuthenticationStateProvider? _authenticationStateProvider = authenticationStateProvider;
    private readonly ILogger<CurrentPrincipalAccessor> _logger = logger;

    public async Task<ClaimsPrincipal?> GetCurrentPrincipalAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var httpUser = _httpContextAccessor.HttpContext?.User;
        if (httpUser?.Identity?.IsAuthenticated == true)
        {
            return httpUser;
        }

        if (_authenticationStateProvider is null)
        {
            return httpUser;
        }

        try
        {
            var authenticationState = await _authenticationStateProvider.GetAuthenticationStateAsync().ConfigureAwait(false);
            return authenticationState?.User ?? httpUser;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogDebug(ex, "Authentication state was not available outside a Blazor circuit.");
            return httpUser;
        }
    }
}
