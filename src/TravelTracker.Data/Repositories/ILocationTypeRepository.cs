using TravelTracker.Data.Models;

namespace TravelTracker.Data.Repositories;

public interface ILocationTypeRepository
{
    Task<IEnumerable<LocationType>> GetAllAsync();
    Task<LocationType?> GetByIdAsync(int id);
    Task<LocationType?> GetByNameAsync(string name);
}
