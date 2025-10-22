using TravelTracker.Data.Models;

namespace TravelTracker.Services.Interfaces;

public interface ILocationService
{
    Task<Location?> GetLocationByIdAsync(int id, int userId);
    Task<IEnumerable<Location>> GetAllLocationsAsync(int userId);
    Task<IEnumerable<Location>> GetLocationsByDateRangeAsync(int userId, DateTime startDate, DateTime endDate);
    Task<IEnumerable<Location>> GetLocationsByStateAsync(int userId, string state);
    Task<Location> CreateLocationAsync(Location location);
    Task<Location> UpdateLocationAsync(Location location);
    Task DeleteLocationAsync(int id, int userId);
    Task<Dictionary<string, int>> GetLocationsByStateCountAsync(int userId);
}
