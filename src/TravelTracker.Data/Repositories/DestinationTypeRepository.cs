namespace TravelTracker.Data.Repositories;

public class DestinationTypeRepository : IDestinationTypeRepository
{
    private readonly TravelTrackerDbContext _context;

    public DestinationTypeRepository(TravelTrackerDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<DestinationType>> GetAllAsync()
    {
        _ = await Task.FromResult(true);
        var types = _context.DestinationTypes.ToList();
        return types;
    }

    public async Task<DestinationType?> GetByIdAsync(int id)
    {
        _ = await Task.FromResult(true);
        var type = _context.DestinationTypes.FirstOrDefault(dt => dt.Id == id);
        return type;
    }

    public async Task<DestinationType?> GetByNameAsync(string name)
    {
        _ = await Task.FromResult(true);
        var type = _context.DestinationTypes.FirstOrDefault(dt => dt.Name == name);
        return type;
    }
}
