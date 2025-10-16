using TravelTracker.Data.Models;

namespace TravelTracker.Services.Interfaces;

public interface ILocationService
{
    Task<Location?> GetLocationByIdAsync(string id, string userId);
    Task<IEnumerable<Location>> GetAllLocationsAsync(string userId);
    Task<IEnumerable<Location>> GetLocationsByDateRangeAsync(string userId, DateTime startDate, DateTime endDate);
    Task<IEnumerable<Location>> GetLocationsByStateAsync(string userId, string state);
    Task<Location> CreateLocationAsync(Location location);
    Task<Location> UpdateLocationAsync(Location location);
    Task DeleteLocationAsync(string id, string userId);
    Task<Dictionary<string, int>> GetLocationsByStateCountAsync(string userId);
}
