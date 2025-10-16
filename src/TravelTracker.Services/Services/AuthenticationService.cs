using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using TravelTracker.Services.Interfaces;

namespace TravelTracker.Services.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserService _userService;

    public AuthenticationService(IHttpContextAccessor httpContextAccessor, IUserService userService)
    {
        _httpContextAccessor = httpContextAccessor;
        _userService = userService;
    }

    public string GetCurrentUserId()
    {
        var entraId = GetCurrentUserEntraId();
        if (string.IsNullOrEmpty(entraId))
        {
            // For local development without authentication, return a test user ID
            return "TEST_USER_ID";
        }

        // Try to get the user from the database, or create if doesn't exist
        var displayName = GetCurrentUserDisplayName();
        var email = GetCurrentUserEmail();
        var user = _userService.GetOrCreateUserAsync(entraId, displayName, email).GetAwaiter().GetResult();

        return user?.Id ?? "TEST_USER_ID";
    }

    public string GetCurrentUserEntraId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return string.Empty;
        }

        // Try to get the object identifier (oid) claim which is the unique ID in Entra ID
        return user.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
            ?? user.FindFirst("oid")?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? string.Empty;
    }

    public string GetCurrentUserDisplayName()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return "Test User";
        }

        return user.FindFirst("name")?.Value
            ?? user.FindFirst(ClaimTypes.Name)?.Value
            ?? user.Identity.Name
            ?? "Unknown User";
    }

    public string GetCurrentUserEmail()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return "test@example.com";
        }

        return user.FindFirst("email")?.Value
            ?? user.FindFirst(ClaimTypes.Email)?.Value
            ?? user.FindFirst("preferred_username")?.Value
            ?? string.Empty;
    }
}
