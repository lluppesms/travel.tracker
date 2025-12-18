namespace TravelTracker.Data.Repositories;

public class LocationRepository : ILocationRepository
{
    private readonly TravelTrackerDbContext _context;

    public LocationRepository(TravelTrackerDbContext context)
    {
        _context = context;
    }

    public async Task<Location?> GetByIdAsync(int id, int userId)
    {
        var location = await _context.Locations
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId);

        if (location != null)
        {
            DeserializeTags(location);
        }

        return location;
    }

    public async Task<IEnumerable<Location>> GetAllByUserIdAsync(int userId)
    {
        try
        {
            var locations = await _context.Locations
                .AsNoTracking()
                .Where(l => l.UserId == userId)
                .ToListAsync();

            foreach (var location in locations)
            {
                DeserializeTags(location);
            }

            return locations;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"error getting locations for {userId} {ex.Message}");
            return new List<Location>().AsEnumerable();
        }
    }

    public async Task<IEnumerable<Location>> GetByDateRangeAsync(int userId, DateTime startDate, DateTime endDate)
    {
        var locations = await _context.Locations
            .AsNoTracking()
            .Where(l => l.UserId == userId && l.StartDate >= startDate && l.StartDate <= endDate)
            .ToListAsync();

        foreach (var location in locations)
        {
            DeserializeTags(location);
        }

        return locations;
    }

    public async Task<IEnumerable<Location>> GetByStateAsync(int userId, string state)
    {
        var locations = await _context.Locations
            .AsNoTracking()
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
        try
        {
            location.ModifiedDate = DateTime.UtcNow;

            // Get the existing entity from the database
            var existingLocation = await _context.Locations
                .FirstOrDefaultAsync(l => l.Id == location.Id);

            if (existingLocation == null)
            {
                throw new InvalidOperationException($"Location with ID {location.Id} not found.");
            }

            // Update only the properties we want to change, avoiding navigation properties
            existingLocation.UserId = location.UserId;
            existingLocation.Name = location.Name;
            existingLocation.LocationType = location.LocationType;
            existingLocation.LocationTypeId = location.LocationTypeId;
            existingLocation.Address = location.Address;
            existingLocation.City = location.City;
            existingLocation.State = location.State;
            existingLocation.ZipCode = location.ZipCode;
            existingLocation.Latitude = location.Latitude;
            existingLocation.Longitude = location.Longitude;
            existingLocation.StartDate = location.StartDate;
            existingLocation.EndDate = location.EndDate;
            existingLocation.Rating = location.Rating;
            existingLocation.Comments = location.Comments;
            existingLocation.TagsJson = location.TagsJson;
            existingLocation.ModifiedDate = location.ModifiedDate;

            await _context.SaveChangesAsync();
            return existingLocation;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating location {location.Id}: {ex.Message}");
            return null;
        }
    }

    public async Task DeleteAsync(int id, int userId)
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
