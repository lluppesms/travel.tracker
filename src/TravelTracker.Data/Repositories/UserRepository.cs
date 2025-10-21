using Microsoft.EntityFrameworkCore;

namespace TravelTracker.Data.Repositories;

public class UserRepository : IUserRepository
{
    private readonly TravelTrackerDbContext _context;

    public UserRepository(TravelTrackerDbContext context)
    {
        _context = context;
    }

    public async Task<Models.User?> GetByIdAsync(string id)
    {
        return await _context.Users.FindAsync(id);
    }

    public async Task<Models.User?> GetByEntraIdAsync(string entraIdUserId)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.EntraIdUserId == entraIdUserId);
    }

    public async Task<Models.User> CreateAsync(Models.User user)
    {
        user.CreatedDate = DateTime.UtcNow;
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<Models.User> UpdateAsync(Models.User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
        return user;
    }
}
