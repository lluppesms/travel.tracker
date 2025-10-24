namespace TravelTracker.Data.Repositories;

public class UserRepository : IUserRepository
{
    private readonly TravelTrackerDbContext _context;

    public UserRepository(TravelTrackerDbContext context)
    {
        _context = context;
    }

    public async Task<Models.User?> GetByIdAsync(int id)
    {
        _ = await Task.FromResult(true);
        var user = _context.Users.Find(id);
        return user;
    }

    public async Task<Models.User?> GetByEntraIdAsync(string entraIdUserId)
    {
        _ = await Task.FromResult(true);
        var user = _context.Users.FirstOrDefault(u => u.EntraIdUserId == entraIdUserId);
        return user;
    }

    public async Task<Models.User> CreateAsync(Models.User user)
    {
        _ = await Task.FromResult(true);
        user.CreatedDate = DateTime.UtcNow;
        _context.Users.Add(user);
        _context.SaveChanges();
        return user;
    }

    public async Task<Models.User> UpdateAsync(Models.User user)
    {
        _ = await Task.FromResult(true);
        _context.Users.Update(user);
        _context.SaveChanges();
        return user;
    }
}
