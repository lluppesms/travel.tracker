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

    public async Task<Location?> GetLocationByIdAsync(int id, int userId)
    {
        return await _locationRepository.GetByIdAsync(id, userId);
    }

    public async Task<IEnumerable<Location>> GetAllLocationsAsync(int userId)
    {
        return await _locationRepository.GetAllByUserIdAsync(userId);
    }

    public async Task<IEnumerable<Location>> GetLocationsByDateRangeAsync(int userId, DateTime startDate, DateTime endDate)
    {
        return await _locationRepository.GetByDateRangeAsync(userId, startDate, endDate);
    }

    public async Task<IEnumerable<Location>> GetLocationsByStateAsync(int userId, string state)
    {
        return await _locationRepository.GetByStateAsync(userId, state);
    }

    public async Task<Location> CreateLocationAsync(Location location)
    {
        try
        {
            return await _locationRepository.CreateAsync(location);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error importing {location.Name}: {ex.Message}");
            return null;
        }
    }

    public async Task<Location> UpdateLocationAsync(Location location)
    {
        return await _locationRepository.UpdateAsync(location);
    }

    public async Task DeleteLocationAsync(int id, int userId)
    {
        await _locationRepository.DeleteAsync(id, userId);
    }

    public async Task<Dictionary<string, int>> GetLocationsByStateCountAsync(int userId)
    {
        var locations = await _locationRepository.GetAllByUserIdAsync(userId);
        return locations
            .GroupBy(l => l.State)
            .ToDictionary(g => g.Key, g => g.Count());
    }
}
