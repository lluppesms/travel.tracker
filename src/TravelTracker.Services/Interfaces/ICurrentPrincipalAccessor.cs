using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace TravelTracker.Services.Interfaces;

/// <summary>
/// Supplies the ambient authenticated principal for the current execution surface.
/// The web project provides an <c>IHttpContextAccessor</c> based implementation for controllers and an
/// <c>AuthenticationStateProvider</c> based implementation for Blazor Server circuits.
/// </summary>
public interface ICurrentPrincipalAccessor
{
    Task<ClaimsPrincipal?> GetCurrentPrincipalAsync(CancellationToken cancellationToken = default);
}
