using TravelTracker.Data.Models;

namespace TravelTracker.Data.Repositories;

public interface INationalParkRepository
{
    Task<IEnumerable<NationalPark>> GetAllAsync();
    Task<NationalPark?> GetByIdAsync(string id, string state);
    Task<IEnumerable<NationalPark>> GetByStateAsync(string state);
}
