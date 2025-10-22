using Microsoft.EntityFrameworkCore;
using TravelTracker.Data.Models;

namespace TravelTracker.Data.Repositories;

public class LocationTypeRepository : ILocationTypeRepository
{
    private readonly TravelTrackerDbContext _context;

    public LocationTypeRepository(TravelTrackerDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<LocationType>> GetAllAsync()
    {
        return await _context.LocationTypes.OrderBy(lt => lt.Name).ToListAsync();
    }

    public async Task<LocationType?> GetByIdAsync(int id)
    {
        return await _context.LocationTypes.FirstOrDefaultAsync(lt => lt.Id == id);
    }

    public async Task<LocationType?> GetByNameAsync(string name)
    {
        return await _context.LocationTypes.FirstOrDefaultAsync(lt => lt.Name == name);
    }
}
