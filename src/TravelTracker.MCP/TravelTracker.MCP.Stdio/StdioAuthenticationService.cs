using Microsoft.Extensions.Configuration;
using TravelTracker.Data.Models;
using TravelTracker.Services.Interfaces;

namespace TravelTracker.Mcp.Stdio;

public sealed class StdioAuthenticationService : IAuthenticationService
{
    private readonly IConfiguration _configuration;
    private readonly IUserService _userService;

    public StdioAuthenticationService(IConfiguration configuration, IUserService userService)
    {
        _configuration = configuration;
        _userService = userService;
    }

    public (int UserId, string? ErrorMessage) ValidateUserAccess(int requestedUserId)
    {
        var currentUserId = GetCurrentUserInternalId();
        if (currentUserId == 0)
        {
            return (0, HttpMessages.UnauthenticatedUser);
        }

        if (currentUserId != requestedUserId)
        {
            return (0, HttpMessages.UnauthorizedUser);
        }

        return (requestedUserId, HttpMessages.UnknownUser);
    }

    public int GetCurrentUserInternalId()
    {
        var configuredUserId = _configuration["ApiKey_UserID"];
        if (!int.TryParse(configuredUserId, out var userId) || userId <= 0)
        {
            return 0;
        }

        var user = _userService.GetUserByIdAsync(userId).GetAwaiter().GetResult();
        return user?.Id ?? 0;
    }

    public bool IsGlobalApiKeyUser()
    {
        return false;
    }

    public string GetCurrentUserId()
    {
        var userId = GetCurrentUserInternalId();
        return userId == 0 ? string.Empty : userId.ToString();
    }

    public string GetCurrentUserEntraId()
    {
        var userId = GetCurrentUserInternalId();
        if (userId == 0)
        {
            return string.Empty;
        }

        var user = _userService.GetUserByIdAsync(userId).GetAwaiter().GetResult();
        return user?.EntraIdUserId ?? string.Empty;
    }

    public string GetCurrentUserDisplayName()
    {
        var userId = GetCurrentUserInternalId();
        if (userId == 0)
        {
            return "Unknown User";
        }

        var user = _userService.GetUserByIdAsync(userId).GetAwaiter().GetResult();
        return user?.Username ?? "Unknown User";
    }

    public string GetCurrentUserEmail()
    {
        var userId = GetCurrentUserInternalId();
        if (userId == 0)
        {
            return string.Empty;
        }

        var user = _userService.GetUserByIdAsync(userId).GetAwaiter().GetResult();
        return user?.Email ?? string.Empty;
    }
}
