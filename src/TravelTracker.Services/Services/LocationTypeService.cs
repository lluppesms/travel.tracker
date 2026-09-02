using TravelTracker.Services.Models;

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

    public async Task<LocationTypeResolutionResult> ResolveLocationTypeAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return new LocationTypeResolutionResult { Status = LocationTypeResolutionStatus.NotFound };
        }

        cancellationToken.ThrowIfCancellationRequested();
        var normalized = name.Trim();
        var types = (await _locationTypeRepository.GetAllAsync()).ToArray();
        cancellationToken.ThrowIfCancellationRequested();

        var exact = types.SingleOrDefault(
            type => string.Equals(type.Name, normalized, StringComparison.OrdinalIgnoreCase));
        if (exact is not null)
        {
            return new LocationTypeResolutionResult
            {
                Status = LocationTypeResolutionStatus.Found,
                LocationType = exact,
                Matches = [exact.Name]
            };
        }

        var partial = types
            .Where(type => type.Name.Contains(normalized, StringComparison.OrdinalIgnoreCase)
                || normalized.Contains(type.Name, StringComparison.OrdinalIgnoreCase))
            .OrderBy(type => type.Name, StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .ToArray();

        return partial.Length switch
        {
            0 => new LocationTypeResolutionResult { Status = LocationTypeResolutionStatus.NotFound },
            1 => new LocationTypeResolutionResult
            {
                Status = LocationTypeResolutionStatus.Found,
                LocationType = partial[0],
                Matches = [partial[0].Name]
            },
            _ => new LocationTypeResolutionResult
            {
                Status = LocationTypeResolutionStatus.Ambiguous,
                Matches = partial.Select(type => type.Name).ToArray()
            }
        };
    }
}
