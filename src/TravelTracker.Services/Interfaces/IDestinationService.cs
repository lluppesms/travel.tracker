using TravelTracker.Data.Models;

namespace TravelTracker.Services.Interfaces;

public interface IDestinationService
{
    Task<IEnumerable<Destination>> GetAllDestinationsAsync();
    Task<Destination?> GetDestinationByIdAsync(int id);
    Task<IEnumerable<Destination>> GetDestinationsByStateAsync(string state);
    Task<IEnumerable<Destination>> GetDestinationsByTypeIdAsync(int destinationTypeId);
    Task<IEnumerable<Destination>> GetDestinationsByTypeNameAsync(string destinationTypeName);
    Task<IEnumerable<Destination>> GetVisitedDestinationsAsync(int userId, int? destinationTypeId = null);
    Task<IEnumerable<DestinationType>> GetAllDestinationTypesAsync();
}
