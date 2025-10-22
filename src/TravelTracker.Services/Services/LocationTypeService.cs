using TravelTracker.Data.Models;
using TravelTracker.Data.Repositories;
using TravelTracker.Services.Interfaces;

namespace TravelTracker.Services.Services;

public class LocationTypeService : ILocationTypeService
{
    private readonly ILocationTypeRepository _locationTypeRepository;

    public LocationTypeService(ILocationTypeRepository locationTypeRepository)
    {
        _locationTypeRepository = locationTypeRepository;
    }

    public async Task<IEnumerable<LocationType>> GetAllLocationTypesAsync()
    {
        return await _locationTypeRepository.GetAllAsync();
    }

    public async Task<LocationType?> GetLocationTypeByIdAsync(int id)
    {
        return await _locationTypeRepository.GetByIdAsync(id);
    }

    public async Task<LocationType?> GetLocationTypeByNameAsync(string name)
    {
        return await _locationTypeRepository.GetByNameAsync(name);
    }

    public async Task<bool> IsValidLocationTypeAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;
            
        var locationType = await _locationTypeRepository.GetByNameAsync(name);
        return locationType != null;
    }
}
