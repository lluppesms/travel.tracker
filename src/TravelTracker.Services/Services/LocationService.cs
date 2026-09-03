using Microsoft.Extensions.Logging;
using TravelTracker.Services.Models;

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

    public async Task<Location> CreateLocationAsync(
        Location location,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(location);
        cancellationToken.ThrowIfCancellationRequested();

        await ValidateLocationAsync(location);
        var created = await _locationRepository.CreateAsync(location, cancellationToken);
        if (created.Id <= 0)
        {
            throw new InvalidOperationException("The location repository did not return a persisted location ID.");
        }

        return created;
    }

    public async Task<Location> UpdateLocationAsync(Location location)
    {
        await ValidateLocationAsync(location);
        return await _locationRepository.UpdateAsync(location) ?? new Location();
    }

    public async Task DeleteLocationAsync(int id, int userId)
    {
        await _locationRepository.DeleteAsync(id, userId);
    }

    public async Task DeleteAllLocationsAsync(int userId)
    {
        await _locationRepository.DeleteAllByUserIdAsync(userId);
    }

    public async Task<Dictionary<string, int>> GetLocationsByStateCountAsync(int userId)
    {
        var locations = await _locationRepository.GetAllByUserIdAsync(userId);
        return locations
            .GroupBy(l => l.State)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    public async Task<IReadOnlyList<AssistantLocationSearchResult>> SearchForAssistantAsync(
        int userId,
        string query,
        int limit = 25,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var boundedLimit = Math.Clamp(limit, 1, 25);
        var locations = await _locationRepository.SearchForAssistantAsync(
            userId,
            query,
            boundedLimit,
            cancellationToken);

        return locations.Select(location => new AssistantLocationSearchResult
        {
            LocationId = location.Id,
            Name = location.Name,
            LocationType = location.LocationType,
            City = location.City,
            State = location.State,
            VisitDate = DateOnly.FromDateTime(location.StartDate)
        }).ToArray();
    }

    public Task<Location?> FindDuplicateAsync(
        int userId,
        string name,
        DateOnly visitDate,
        string? city,
        string? state,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A location name is required.", nameof(name));
        }

        return _locationRepository.FindDuplicateAsync(
            userId,
            name.Trim(),
            visitDate.ToDateTime(TimeOnly.MinValue),
            string.IsNullOrWhiteSpace(city) ? null : city.Trim(),
            string.IsNullOrWhiteSpace(state) ? null : state.Trim(),
            cancellationToken);
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
