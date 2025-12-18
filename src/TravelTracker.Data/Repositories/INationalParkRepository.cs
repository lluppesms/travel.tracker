namespace TravelTracker.Data.Repositories;

public interface INationalParkRepository
{
    Task<IEnumerable<NationalPark>> GetAllAsync();
    Task<NationalPark?> GetByIdAsync(int id);
    Task<IEnumerable<NationalPark>> GetByStateAsync(string state);
}
