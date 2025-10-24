using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;

namespace TravelTracker.Services.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserService _userService;
    private readonly string? _apiKey;

    public AuthenticationService(IHttpContextAccessor httpContextAccessor, IUserService userService, IConfiguration configuration)
    {
        _httpContextAccessor = httpContextAccessor;
        _userService = userService;
        _apiKey = configuration["ApiKey"];
    }

    public int GetCurrentUserInternalId()
    {
        var entraId = GetCurrentUserEntraId();
        if (string.IsNullOrEmpty(entraId))
        {
            // check for valid apikey and userid header
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null
                && httpContext.Request.Headers.TryGetValue("X-API-Key", out var suppliedApiKey)
                && httpContext.Request.Headers.TryGetValue("X-User-Id", out var userIdHeader)
                && httpContext.Request.Headers.TryGetValue("X-User-Email", out var userEmailHeader))
            {
                if (suppliedApiKey == _apiKey)
                {
                    if (int.TryParse(userIdHeader, out var userId))
                    {
                        var _requestedUser = _userService.GetUserByIdAsync(userId).GetAwaiter().GetResult();
                        if (_requestedUser != null && _requestedUser.Email == userEmailHeader)
                        {
                            return userId;
                        }
                    }
                }
            }
            return 0;
        }
        //if (string.IsNullOrEmpty(entraId))
        //{
        //    // fallback test id
        //    var testUser = _userService.GetOrCreateUserAsync("TEST_USER_ENTRA", "Test User", "test@example.com").GetAwaiter().GetResult();
        //    return testUser.Id;
        //}
        var displayName = GetCurrentUserDisplayName();
        var email = GetCurrentUserEmail();
        var user = _userService.GetOrCreateUserAsync(entraId, displayName, email).GetAwaiter().GetResult();
        return user.Id;
    }

    // Legacy external id (Entra ID or test) kept for compatibility with existing code paths still using string ids.
    public string GetCurrentUserId()
    {
        var entraId = GetCurrentUserEntraId();
        if (string.IsNullOrEmpty(entraId))
        {
            return "TEST_USER_ENTRA";
        }
        return entraId;
    }

    public string GetCurrentUserEntraId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return string.Empty;
        }

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
