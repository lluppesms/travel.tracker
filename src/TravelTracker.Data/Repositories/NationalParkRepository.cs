using Microsoft.EntityFrameworkCore;
using TravelTracker.Data.Models;

namespace TravelTracker.Data.Repositories;

public class NationalParkRepository : INationalParkRepository
{
    private readonly TravelTrackerDbContext _context;

    public NationalParkRepository(TravelTrackerDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<NationalPark>> GetAllAsync()
    {
        return await _context.NationalParks.ToListAsync();
    }

    public async Task<NationalPark?> GetByIdAsync(string id, string state)
    {
        return await _context.NationalParks
            .FirstOrDefaultAsync(np => np.Id == id && np.State == state);
    }

    public async Task<IEnumerable<NationalPark>> GetByStateAsync(string state)
    {
        return await _context.NationalParks
            .Where(np => np.State == state)
            .ToListAsync();
    }
}
