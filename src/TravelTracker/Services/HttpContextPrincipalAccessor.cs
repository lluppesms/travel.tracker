using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

using TravelTracker.Services.Interfaces;

namespace TravelTracker.Services;

/// <summary>
/// Supplies the authenticated principal from the current HTTP request. Register this accessor for the
/// controller/API surface; it is not valid as circuit identity for Blazor Server components.
/// </summary>
public class HttpContextPrincipalAccessor(IHttpContextAccessor httpContextAccessor) : ICurrentPrincipalAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public Task<ClaimsPrincipal?> GetCurrentPrincipalAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<ClaimsPrincipal?>(_httpContextAccessor.HttpContext?.User);
    }
}
