namespace TravelTracker.Services.Interfaces;

public interface IAuthenticationService
{
    (int UserId, string? ErrorMessage) ValidateUserAccess(int requestedUserId);
    int GetCurrentUserInternalId();
    bool IsGlobalApiKeyUser();
    string GetCurrentUserId();
    string GetCurrentUserEntraId();
    string GetCurrentUserDisplayName();
    string GetCurrentUserEmail();
}
