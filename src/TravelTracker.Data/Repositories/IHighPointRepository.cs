namespace TravelTracker.Data.Repositories;

public interface IHighPointRepository
{
    Task<IEnumerable<HighPoint>> GetAllAsync();
    Task<HighPoint?> GetByIdAsync(int id);
    Task<IEnumerable<HighPoint>> GetByStateAsync(string state);
}
