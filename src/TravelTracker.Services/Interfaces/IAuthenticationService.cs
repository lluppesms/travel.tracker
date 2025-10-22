namespace TravelTracker.Services.Interfaces;

public interface IAuthenticationService
{
    int GetCurrentUserInternalId();
    string GetCurrentUserId();
    string GetCurrentUserEntraId();
    string GetCurrentUserDisplayName();
    string GetCurrentUserEmail();
}
