namespace TravelTracker.Services.Services;

public class DestinationService : IDestinationService
{
    private readonly IDestinationRepository _destinationRepository;
    private readonly IDestinationTypeRepository _destinationTypeRepository;
    private readonly ILocationRepository _locationRepository;

    public DestinationService(
        IDestinationRepository destinationRepository,
        IDestinationTypeRepository destinationTypeRepository,
        ILocationRepository locationRepository)
    {
        _destinationRepository = destinationRepository;
        _destinationTypeRepository = destinationTypeRepository;
        _locationRepository = locationRepository;
    }

    public async Task<IEnumerable<Destination>> GetAllDestinationsAsync()
    {
        return await _destinationRepository.GetAllAsync();
    }

    public async Task<Destination?> GetDestinationByIdAsync(int id)
    {
        return await _destinationRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<Destination>> GetDestinationsByStateAsync(string state)
    {
        return await _destinationRepository.GetByStateAsync(state);
    }

    public async Task<IEnumerable<Destination>> GetDestinationsByTypeIdAsync(int destinationTypeId)
    {
        return await _destinationRepository.GetByDestinationTypeIdAsync(destinationTypeId);
    }

    public async Task<IEnumerable<Destination>> GetDestinationsByTypeNameAsync(string destinationTypeName)
    {
        return await _destinationRepository.GetByDestinationTypeNameAsync(destinationTypeName);
    }

    public async Task<IEnumerable<Destination>> GetVisitedDestinationsAsync(int userId, int? destinationTypeId = null)
    {
        var allDestinations = destinationTypeId.HasValue
            ? await _destinationRepository.GetByDestinationTypeIdAsync(destinationTypeId.Value)
            : await _destinationRepository.GetAllAsync();
        
        var userLocations = await _locationRepository.GetAllByUserIdAsync(userId);

        // Map location types to destination types
        var locationTypeMapping = new Dictionary<string, string>
        {
            { "National Park", "National Park" },
            { "State High Point", "State High Point" },
            { "Presidential Library", "Presidential Library" }
        };

        var visitedDestinations = allDestinations
            .Where(dest =>
            {
                // Find matching user locations based on name similarity
                return userLocations.Any(loc =>
                {
                    // Check if location type matches destination type
                    if (locationTypeMapping.TryGetValue(loc.LocationType ?? "", out var destTypeName))
                    {
                        // Compare names
                        return loc.Name.Contains(dest.Name, StringComparison.OrdinalIgnoreCase) ||
                               dest.Name.Contains(loc.Name, StringComparison.OrdinalIgnoreCase);
                    }
                    return false;
                });
            })
            .ToList();

        return visitedDestinations;
    }

    public async Task<IEnumerable<DestinationType>> GetAllDestinationTypesAsync()
    {
        return await _destinationTypeRepository.GetAllAsync();
    }
}
