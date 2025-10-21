using Microsoft.EntityFrameworkCore;
using TravelTracker.Data.Models;
using System.Text.Json;

namespace TravelTracker.Data.Repositories;

public class LocationRepository : ILocationRepository
{
    private readonly TravelTrackerDbContext _context;

    public LocationRepository(TravelTrackerDbContext context)
    {
        _context = context;
    }

    public async Task<Location?> GetByIdAsync(string id, string userId)
    {
        _ = await Task.FromResult(true);
        var location = _context.Locations
            .FirstOrDefault(l => l.Id == id && l.UserId == userId);

        if (location != null)
        {
            DeserializeTags(location);
        }

        return location;
    }

    public async Task<IEnumerable<Location>> GetAllByUserIdAsync(string userId)
    {
        _ = await Task.FromResult(true);
        var locations = _context.Locations
            .Where(l => l.UserId == userId)
            .ToList();

        foreach (var location in locations)
        {
            DeserializeTags(location);
        }

        return locations;
    }

    public async Task<IEnumerable<Location>> GetByDateRangeAsync(string userId, DateTime startDate, DateTime endDate)
    {
        var locations = _context.Locations
            .Where(l => l.UserId == userId && l.StartDate >= startDate && l.StartDate <= endDate)
            .ToList();

        foreach (var location in locations)
        {
            DeserializeTags(location);
        }

        return locations;
    }

    public async Task<IEnumerable<Location>> GetByStateAsync(string userId, string state)
    {
        _ = await Task.FromResult(true);
        var locations = _context.Locations
            .Where(l => l.UserId == userId && l.State == state)
            .ToList();

        foreach (var location in locations)
        {
            DeserializeTags(location);
        }

        return locations;
    }

    public async Task<Location> CreateAsync(Location location)
    {
        _ = await Task.FromResult(true);
        location.CreatedDate = DateTime.UtcNow;
        location.ModifiedDate = DateTime.UtcNow;
        _context.Locations.Add(location);
        _context.SaveChanges();
        return location;
    }

    public async Task<Location> UpdateAsync(Location location)
    {
        location.ModifiedDate = DateTime.UtcNow;
        _context.Locations.Update(location);
        _context.SaveChanges();
        return location;
    }

    public async Task DeleteAsync(string id, string userId)
    {
        _ = await Task.FromResult(true);
        var location = _context.Locations
            .FirstOrDefault(l => l.Id == id && l.UserId == userId);

        if (location != null)
        {
            _context.Locations.Remove(location);
            _context.SaveChanges();
        }
    }

    private void DeserializeTags(Location location)
    {
        if (!string.IsNullOrEmpty(location.TagsJson))
        {
            try
            {
                location.Tags = JsonSerializer.Deserialize<List<string>>(location.TagsJson) ?? new List<string>();
            }
            catch
            {
                location.Tags = new List<string>();
            }
        }
    }
}
