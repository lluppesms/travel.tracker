using TravelTracker.Data.Models;

namespace TravelTracker.Data.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(string id);
    Task<User?> GetByEntraIdAsync(string entraIdUserId);
    Task<User> CreateAsync(User user);
    Task<User> UpdateAsync(User user);
}
