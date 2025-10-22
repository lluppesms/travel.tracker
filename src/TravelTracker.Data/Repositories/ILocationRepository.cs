using TravelTracker.Data.Models;

namespace TravelTracker.Data.Repositories;

public interface ILocationRepository
{
    Task<Location?> GetByIdAsync(int id, int userId);
    Task<IEnumerable<Location>> GetAllByUserIdAsync(int userId);
    Task<IEnumerable<Location>> GetByDateRangeAsync(int userId, DateTime startDate, DateTime endDate);
    Task<IEnumerable<Location>> GetByStateAsync(int userId, string state);
    Task<Location> CreateAsync(Location location);
    Task<Location> UpdateAsync(Location location);
    Task DeleteAsync(int id, int userId);
}
