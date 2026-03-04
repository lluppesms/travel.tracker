using Microsoft.Extensions.Logging;

namespace TravelTracker.Services.Services;

public class LocationService(
    ILocationRepository locationRepository,
    ILocationTypeRepository locationTypeRepository,
    ILogger<LocationService> logger) : ILocationService
{
    private readonly ILocationRepository _locationRepository = locationRepository;
    private readonly ILocationTypeRepository _locationTypeRepository = locationTypeRepository;
    private readonly ILogger<LocationService> _logger = logger;

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
            var message = ex.InnerException != null ? $"{ex.Message} {ex.InnerException.Message}" : ex.Message;
            _logger.LogError(
                ex,
                "Failed to create location '{Name}' for user {UserId}. Type={Type}, City={City}, State={State}, Zip={Zip}, Latitude={Latitude}, Longitude={Longitude}. Details: {Details}",
                location.Name,
                location.UserId,
                location.LocationType,
                location.City,
                location.State,
                location.ZipCode,
                location.Latitude,
                location.Longitude,
                message);
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
    }
}
