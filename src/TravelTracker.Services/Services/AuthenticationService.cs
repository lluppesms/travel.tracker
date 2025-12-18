using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;

namespace TravelTracker.Services.Services;

public class AuthenticationService(IHttpContextAccessor httpContextAccessor, IUserService userService, IConfiguration configuration) : IAuthenticationService
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly IUserService _userService = userService;
    private readonly string? _configurationApiKey = configuration["ApiKey"];
    private readonly string? _configurationApiKeyUserId = configuration["ApiKey_UserID"];
    private readonly string? _configurationApiKeyEmailAddress = configuration["ApiKey_EmailAddress"];

    public int GetCurrentUserInternalId()
    {
        // check to see if the user is already logged in via Entra ID
        var entraId = GetCurrentUserEntraId();
        if (!string.IsNullOrEmpty(entraId))
        {
            var displayName = GetCurrentUserDisplayName();
            var email = GetCurrentUserEmail();
            var user = _userService.GetOrCreateUserAsync(entraId, displayName, email).GetAwaiter().GetResult();
            if (user != null)
            {
                return user.Id;
            }
        }

        // if not entra authenticated, check for valid apikey header
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null && httpContext.Request.Headers.TryGetValue("X-API-Key", out var suppliedApiKey))
        {
            // First, check if the API key matches the config-based API key
            if (!string.IsNullOrEmpty(_configurationApiKey) && suppliedApiKey == _configurationApiKey)
            {
                // Use config-based user credentials
                if (!string.IsNullOrEmpty(_configurationApiKeyUserId) && int.TryParse(_configurationApiKeyUserId, out var configUserId))
                {
                    // Get the user specified in the configuration for the default key
                    var configUser = _userService.GetUserByIdAsync(configUserId).GetAwaiter().GetResult();
                    // Check to make sure the email matches the config email address if specified
                    if (configUser != null && (string.IsNullOrEmpty(_configurationApiKeyEmailAddress) || configUser.Email == _configurationApiKeyEmailAddress))
                    {
                        return configUserId;
                    }
                }
            }
            else
            {
                // Check if the API key matches a specific user record ApiKey in the database
                var userByApiKey = _userService.GetUserByApiKeyAsync(suppliedApiKey.ToString()).GetAwaiter().GetResult();
                if (userByApiKey != null)
                {
                    return userByApiKey.Id;
                }
            }
        }
        return 0;
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
