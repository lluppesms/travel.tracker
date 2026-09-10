namespace TravelTracker.Data.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByEntraIdAsync(string entraIdUserId);
    Task<User?> GetByApiKeyAsync(string apiKey);
    Task<User> CreateAsync(User user);
    Task<User> UpdateAsync(User user);
}
