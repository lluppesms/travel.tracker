namespace TravelTracker.Services.Services;

public class HighPointService : IHighPointService
{
    private readonly IHighPointRepository _highPointRepository;
    private readonly ILocationRepository _locationRepository;

    public HighPointService(IHighPointRepository highPointRepository, ILocationRepository locationRepository)
    {
        _highPointRepository = highPointRepository;
        _locationRepository = locationRepository;
    }

    public async Task<IEnumerable<HighPoint>> GetAllHighPointsAsync()
    {
        return await _highPointRepository.GetAllAsync();
    }

    public async Task<HighPoint?> GetHighPointByIdAsync(int id)
    {
        return await _highPointRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<HighPoint>> GetHighPointsByStateAsync(string state)
    {
        return await _highPointRepository.GetByStateAsync(state);
    }

    public async Task<IEnumerable<HighPoint>> GetVisitedHighPointsAsync(int userId)
    {
        var allHighPoints = await _highPointRepository.GetAllAsync();
        var userLocations = await _locationRepository.GetAllByUserIdAsync(userId);

        var highPointLocations = userLocations
            .Where(l => l.LocationType.Equals("State High Point", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var visitedHighPoints = allHighPoints
            .Where(highPoint => highPointLocations.Any(loc =>
                loc.Name.Contains(highPoint.Name, StringComparison.OrdinalIgnoreCase) ||
                highPoint.Name.Contains(loc.Name, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        return visitedHighPoints;
    }
}
