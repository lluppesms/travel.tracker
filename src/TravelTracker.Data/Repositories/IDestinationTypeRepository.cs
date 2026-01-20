namespace TravelTracker.Data.Repositories;

public interface IDestinationTypeRepository
{
    Task<IEnumerable<DestinationType>> GetAllAsync();
    Task<DestinationType?> GetByIdAsync(int id);
    Task<DestinationType?> GetByNameAsync(string name);
}
