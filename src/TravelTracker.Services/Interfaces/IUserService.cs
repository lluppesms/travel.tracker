using TravelTracker.Data.Models;

namespace TravelTracker.Services.Interfaces;

public interface IUserService
{
    Task<User?> GetUserByIdAsync(string id);
    Task<User?> GetUserByEntraIdAsync(string entraIdUserId);
    Task<User> GetOrCreateUserAsync(string entraIdUserId, string username, string email);
    Task UpdateLastLoginAsync(string userId);
}
