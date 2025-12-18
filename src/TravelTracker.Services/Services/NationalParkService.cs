namespace TravelTracker.Services.Services;

public class NationalParkService : INationalParkService
{
    private readonly INationalParkRepository _nationalParkRepository;
    private readonly ILocationRepository _locationRepository;

    public NationalParkService(INationalParkRepository nationalParkRepository, ILocationRepository locationRepository)
    {
        _nationalParkRepository = nationalParkRepository;
        _locationRepository = locationRepository;
    }

    public async Task<IEnumerable<NationalPark>> GetAllParksAsync()
    {
        return await _nationalParkRepository.GetAllAsync();
    }

    public async Task<NationalPark?> GetParkByIdAsync(int id)
    {
        return await _nationalParkRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<NationalPark>> GetParksByStateAsync(string state)
    {
        return await _nationalParkRepository.GetByStateAsync(state);
    }

    public async Task<IEnumerable<NationalPark>> GetVisitedParksAsync(int userId)
    {
        var allParks = await _nationalParkRepository.GetAllAsync();
        var userLocations = await _locationRepository.GetAllByUserIdAsync(userId);

        var nationalParkLocations = userLocations
            .Where(l => l.LocationType.Equals("National Park", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var visitedParks = allParks
            .Where(park => nationalParkLocations.Any(loc =>
                loc.Name.Contains(park.Name, StringComparison.OrdinalIgnoreCase) ||
                park.Name.Contains(loc.Name, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        return visitedParks;
    }
}
