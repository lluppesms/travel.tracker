using TravelTracker.Data.Models;
using TravelTracker.Services.Models;

namespace TravelTracker.Services.Interfaces;

public interface ILocationTypeService
{
    Task<IEnumerable<LocationType>> GetAllLocationTypesAsync();
    Task<LocationType?> GetLocationTypeByIdAsync(int id);
    Task<LocationType?> GetLocationTypeByNameAsync(string name);
    Task<bool> IsValidLocationTypeAsync(string name);
    Task<LocationTypeResolutionResult> ResolveLocationTypeAsync(
        string name,
        CancellationToken cancellationToken = default);
}
