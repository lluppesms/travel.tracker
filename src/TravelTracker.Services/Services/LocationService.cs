namespace TravelTracker.Services.Services;

public class LocationService(ILocationRepository locationRepository, ILocationTypeRepository locationTypeRepository, INationalParkRepository nationalParkRepository) : ILocationService
{
    private readonly ILocationRepository _locationRepository = locationRepository;
    private readonly ILocationTypeRepository _locationTypeRepository = locationTypeRepository;
    private readonly INationalParkRepository _nationalParkRepository = nationalParkRepository;

    public async Task<Location?> GetLocationByIdAsync(int id, int userId)
    {
        return await _locationRepository.GetByIdAsync(id, userId);
    }

    public async Task<IEnumerable<Location>> GetAllLocationsAsync(int userId)
    {
        return await _locationRepository.GetAllByUserIdAsync(userId);
    }

    public async Task<IEnumerable<Location>> GetLocationsByDateRangeAsync(int userId, DateTime startDate, DateTime endDate)
    {
        return await _locationRepository.GetByDateRangeAsync(userId, startDate, endDate);
    }

    public async Task<IEnumerable<Location>> GetLocationsByStateAsync(int userId, string state)
    {
        return await _locationRepository.GetByStateAsync(userId, state);
    }

    public async Task<Location> CreateLocationAsync(Location location)
    {
        try
        {
            await ValidateLocationAsync(location);
            return await _locationRepository.CreateAsync(location);
        }
        catch (Exception ex)
        {
            var msg = ex.Message;
            msg += ex.InnerException != null ? " " + ex.InnerException.Message : string.Empty;
            Console.WriteLine($"Error importing {location.Name}: {msg}");
            return null;
        }
    }

    public async Task<Location> UpdateLocationAsync(Location location)
    {
        await ValidateLocationAsync(location);
        return await _locationRepository.UpdateAsync(location);
    }

    public async Task DeleteLocationAsync(int id, int userId)
    {
        await _locationRepository.DeleteAsync(id, userId);
    }

    public async Task<Dictionary<string, int>> GetLocationsByStateCountAsync(int userId)
    {
        var locations = await _locationRepository.GetAllByUserIdAsync(userId);
        return locations
            .GroupBy(l => l.State)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    private async Task ValidateLocationAsync(Location location)
    {
        // Validate location type exists in lookup table
        if (string.IsNullOrWhiteSpace(location.LocationType))
        {
            throw new ArgumentException("Location type is required.");
        }

        var locationType = await _locationTypeRepository.GetByNameAsync(location.LocationType);
        if (locationType == null)
        {
            var validTypes = await _locationTypeRepository.GetAllAsync();
            var validTypeNames = string.Join(", ", validTypes.Select(t => t.Name));
            throw new ArgumentException($"Invalid location type '{location.LocationType}'. Valid types are: {validTypeNames}");
        }

        // Set the LocationTypeId for the foreign key relationship
        location.LocationTypeId = locationType.Id;

        // Special validation for National Park type
        if (location.LocationType.Equals("National Park", StringComparison.OrdinalIgnoreCase))
        {
            var allParks = await _nationalParkRepository.GetAllAsync();
            var matchingPark = allParks.FirstOrDefault(park =>
                park.Name.Equals(location.Name, StringComparison.OrdinalIgnoreCase) ||
                park.Name.Contains(location.Name, StringComparison.OrdinalIgnoreCase) ||
                location.Name.Contains(park.Name, StringComparison.OrdinalIgnoreCase));

            if (matchingPark == null)
            {
                throw new ArgumentException($"National Park '{location.Name}' is not found in the National Parks database. Please verify the park name.");
            }
        }
    }
}
