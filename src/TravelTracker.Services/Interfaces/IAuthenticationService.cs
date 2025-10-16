namespace TravelTracker.Services.Interfaces;

public interface IAuthenticationService
{
    string GetCurrentUserId();
    string GetCurrentUserEntraId();
    string GetCurrentUserDisplayName();
    string GetCurrentUserEmail();
}
