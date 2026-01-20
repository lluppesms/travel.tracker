namespace TravelTracker.Data.Repositories;

public interface IDestinationRepository
{
    Task<IEnumerable<Destination>> GetAllAsync();
    Task<Destination?> GetByIdAsync(int id);
    Task<IEnumerable<Destination>> GetByStateAsync(string state);
    Task<IEnumerable<Destination>> GetByDestinationTypeIdAsync(int destinationTypeId);
    Task<IEnumerable<Destination>> GetByDestinationTypeNameAsync(string destinationTypeName);
}
