using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

using TravelTracker.Services.Models;

namespace TravelTracker.Services.Interfaces;

/// <summary>
/// Maps a trusted authenticated <see cref="ClaimsPrincipal"/> to exactly one internal Travel Tracker user.
/// Scoped lifetime. No method accepts a caller-supplied user id, and the global API key can never
/// select a user on the assistant surface.
/// </summary>
public interface ICurrentTravelUserResolver
{
    /// <summary>
    /// Resolves the internal user for the supplied principal. Returns <c>null</c> when the principal is
    /// missing, unauthenticated, or has no usable external identifier.
    /// </summary>
    Task<TravelAssistantUserContext?> ResolveAsync(ClaimsPrincipal? principal, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the internal user for the ambient principal supplied by <see cref="ICurrentPrincipalAccessor"/>
    /// (HTTP context for controllers, authentication state for Blazor circuits).
    /// </summary>
    Task<TravelAssistantUserContext?> ResolveCurrentAsync(CancellationToken cancellationToken = default);
}
