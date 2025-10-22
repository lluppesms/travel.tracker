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
        _ = await Task.FromResult(true);
        var parks = _context.NationalParks.ToList();
        return parks;
    }

    public async Task<NationalPark?> GetByIdAsync(int id, string state)
    {
        _ = await Task.FromResult(true);
        var park = _context.NationalParks
            .FirstOrDefault(np => np.Id == id && np.State == state);
        return park;
    }

    public async Task<IEnumerable<NationalPark>> GetByStateAsync(string state)
    {
        _ = await Task.FromResult(true);
        var parks = _context.NationalParks
            .Where(np => np.State == state)
            .ToList();
        return parks;
    }
}
