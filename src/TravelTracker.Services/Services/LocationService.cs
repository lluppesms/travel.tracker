using TravelTracker.Data.Models;
using TravelTracker.Data.Repositories;
using TravelTracker.Services.Interfaces;

namespace TravelTracker.Services.Services;

public class LocationService : ILocationService
{
    private readonly ILocationRepository _locationRepository;

    public LocationService(ILocationRepository locationRepository)
    {
        _locationRepository = locationRepository;
    }

    public async Task<Location?> GetLocationByIdAsync(string id, string userId)
    {
        return await _locationRepository.GetByIdAsync(id, userId);
    }

    public async Task<IEnumerable<Location>> GetAllLocationsAsync(string userId)
    {
        return await _locationRepository.GetAllByUserIdAsync(userId);
    }

    public async Task<IEnumerable<Location>> GetLocationsByDateRangeAsync(string userId, DateTime startDate, DateTime endDate)
    {
        return await _locationRepository.GetByDateRangeAsync(userId, startDate, endDate);
    }

    public async Task<IEnumerable<Location>> GetLocationsByStateAsync(string userId, string state)
    {
        return await _locationRepository.GetByStateAsync(userId, state);
    }

    public async Task<Location> CreateLocationAsync(Location location)
    {
        return await _locationRepository.CreateAsync(location);
    }

    public async Task<Location> UpdateLocationAsync(Location location)
    {
        return await _locationRepository.UpdateAsync(location);
    }

    public async Task DeleteLocationAsync(string id, string userId)
    {
        await _locationRepository.DeleteAsync(id, userId);
    }

    public async Task<Dictionary<string, int>> GetLocationsByStateCountAsync(string userId)
    {
        var locations = await _locationRepository.GetAllByUserIdAsync(userId);
        return locations
            .GroupBy(l => l.State)
            .ToDictionary(g => g.Key, g => g.Count());
    }
}
