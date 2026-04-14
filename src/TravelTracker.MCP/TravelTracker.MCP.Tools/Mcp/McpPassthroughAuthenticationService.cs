namespace TravelTracker.Mcp;

public sealed class McpPassthroughAuthenticationService : IAuthenticationService
{
    private const string UnauthenticatedUserMessage = "User is not authenticated.";

    private readonly IConfiguration _configuration;

    public McpPassthroughAuthenticationService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public (int UserId, string? ErrorMessage) ValidateUserAccess(int requestedUserId)
    {
        if (requestedUserId <= 0)
        {
            return (0, UnauthenticatedUserMessage);
        }

        // In MCP API-first mode, downstream API authorization validates the user context.
        return (requestedUserId, null);
    }

    public int GetCurrentUserInternalId()
    {
        var configuredUserId = _configuration["ApiKey_UserID"];
        return int.TryParse(configuredUserId, out var userId) && userId > 0 ? userId : 0;
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
        return string.Empty;
    }

    public string GetCurrentUserDisplayName()
    {
        return "MCP User";
    }

    public string GetCurrentUserEmail()
    {
        return _configuration["ApiKey_EmailAddress"] ?? string.Empty;
    }
}
