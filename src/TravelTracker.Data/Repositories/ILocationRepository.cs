using TravelTracker.Data.Models;

namespace TravelTracker.Data.Repositories;

public interface ILocationRepository
{
    Task<Location?> GetByIdAsync(string id, string userId);
    Task<IEnumerable<Location>> GetAllByUserIdAsync(string userId);
    Task<IEnumerable<Location>> GetByDateRangeAsync(string userId, DateTime startDate, DateTime endDate);
    Task<IEnumerable<Location>> GetByStateAsync(string userId, string state);
    Task<Location> CreateAsync(Location location);
    Task<Location> UpdateAsync(Location location);
    Task DeleteAsync(string id, string userId);
}
