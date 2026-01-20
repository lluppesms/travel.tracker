namespace TravelTracker.Data.Repositories;

public class DestinationRepository : IDestinationRepository
{
    private readonly TravelTrackerDbContext _context;

    public DestinationRepository(TravelTrackerDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Destination>> GetAllAsync()
    {
        _ = await Task.FromResult(true);
        var destinations = _context.Destinations.ToList();
        return destinations;
    }

    public async Task<Destination?> GetByIdAsync(int id)
    {
        _ = await Task.FromResult(true);
        var destination = _context.Destinations.FirstOrDefault(d => d.Id == id);
        return destination;
    }

    public async Task<IEnumerable<Destination>> GetByStateAsync(string state)
    {
        _ = await Task.FromResult(true);
        var destinations = _context.Destinations
            .Where(d => d.State == state)
            .ToList();
        return destinations;
    }

    public async Task<IEnumerable<Destination>> GetByDestinationTypeIdAsync(int destinationTypeId)
    {
        _ = await Task.FromResult(true);
        var destinations = _context.Destinations
            .Where(d => d.DestinationTypeId == destinationTypeId)
            .ToList();
        return destinations;
    }

    public async Task<IEnumerable<Destination>> GetByDestinationTypeNameAsync(string destinationTypeName)
    {
        _ = await Task.FromResult(true);
        var destinations = _context.Destinations
            .Where(d => d.DestinationType != null && d.DestinationType.Name == destinationTypeName)
            .ToList();
        return destinations;
    }
}
