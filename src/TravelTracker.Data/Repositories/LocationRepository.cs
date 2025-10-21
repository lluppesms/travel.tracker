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
        var location = await _context.Locations
            .FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId);
        
        if (location != null)
        {
            DeserializeTags(location);
        }
        
        return location;
    }

    public async Task<IEnumerable<Location>> GetAllByUserIdAsync(string userId)
    {
        var locations = await _context.Locations
            .Where(l => l.UserId == userId)
            .ToListAsync();
        
        foreach (var location in locations)
        {
            DeserializeTags(location);
        }
        
        return locations;
    }

    public async Task<IEnumerable<Location>> GetByDateRangeAsync(string userId, DateTime startDate, DateTime endDate)
    {
        var locations = await _context.Locations
            .Where(l => l.UserId == userId && l.StartDate >= startDate && l.StartDate <= endDate)
            .ToListAsync();
        
        foreach (var location in locations)
        {
            DeserializeTags(location);
        }
        
        return locations;
    }

    public async Task<IEnumerable<Location>> GetByStateAsync(string userId, string state)
    {
        var locations = await _context.Locations
            .Where(l => l.UserId == userId && l.State == state)
            .ToListAsync();
        
        foreach (var location in locations)
        {
            DeserializeTags(location);
        }
        
        return locations;
    }

    public async Task<Location> CreateAsync(Location location)
    {
        location.CreatedDate = DateTime.UtcNow;
        location.ModifiedDate = DateTime.UtcNow;
        _context.Locations.Add(location);
        await _context.SaveChangesAsync();
        return location;
    }

    public async Task<Location> UpdateAsync(Location location)
    {
        location.ModifiedDate = DateTime.UtcNow;
        _context.Locations.Update(location);
        await _context.SaveChangesAsync();
        return location;
    }

    public async Task DeleteAsync(string id, string userId)
    {
        var location = await _context.Locations
            .FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId);
        
        if (location != null)
        {
            _context.Locations.Remove(location);
            await _context.SaveChangesAsync();
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
