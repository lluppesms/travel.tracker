using TravelTracker.Data.Models;
using TravelTracker.Data.Repositories;
using TravelTracker.Services.Interfaces;

namespace TravelTracker.Services.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<User?> GetUserByIdAsync(string id)
    {
        return await _userRepository.GetByIdAsync(id);
    }

    public async Task<User?> GetUserByEntraIdAsync(string entraIdUserId)
    {
        return await _userRepository.GetByEntraIdAsync(entraIdUserId);
    }

    public async Task<User> GetOrCreateUserAsync(string entraIdUserId, string username, string email)
    {
        var user = await _userRepository.GetByEntraIdAsync(entraIdUserId);

        if (user == null)
        {
            user = new User
            {
                Id = entraIdUserId,
                EntraIdUserId = entraIdUserId,
                Username = username,
                Email = email,
                CreatedDate = DateTime.UtcNow,
                LastLoginDate = DateTime.UtcNow
            };

            user = await _userRepository.CreateAsync(user);
        }
        else
        {
            user.LastLoginDate = DateTime.UtcNow;
            user = await _userRepository.UpdateAsync(user);
        }

        return user;
    }

    public async Task UpdateLastLoginAsync(string userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user != null)
        {
            user.LastLoginDate = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);
        }
    }
}
