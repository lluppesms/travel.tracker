using TravelTracker.Data.Models;

namespace TravelTracker.Services.Interfaces;

public interface IUserService
{
    Task<User?> GetUserByIdAsync(int id);
    Task<User?> GetUserByEntraIdAsync(string entraIdUserId);
    Task<User> GetOrCreateUserAsync(string entraIdUserId, string username, string email);
    Task UpdateLastLoginAsync(int userId);
}
