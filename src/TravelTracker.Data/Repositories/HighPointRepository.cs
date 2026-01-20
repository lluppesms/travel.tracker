namespace TravelTracker.Data.Repositories;

public class HighPointRepository : IHighPointRepository
{
    private readonly TravelTrackerDbContext _context;

    public HighPointRepository(TravelTrackerDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<HighPoint>> GetAllAsync()
    {
        _ = await Task.FromResult(true);
        var highPoints = _context.HighPoints.ToList();
        return highPoints;
    }

    public async Task<HighPoint?> GetByIdAsync(int id)
    {
        _ = await Task.FromResult(true);
        var highPoint = _context.HighPoints.FirstOrDefault(hp => hp.Id == id);
        return highPoint;
    }

    public async Task<IEnumerable<HighPoint>> GetByStateAsync(string state)
    {
        _ = await Task.FromResult(true);
        var highPoints = _context.HighPoints
            .Where(hp => hp.State == state)
            .ToList();
        return highPoints;
    }
}
