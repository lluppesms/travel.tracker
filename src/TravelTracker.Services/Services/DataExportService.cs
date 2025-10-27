using System.Text;
using System.Text.Json;

namespace TravelTracker.Services.Services;

public class DataExportService : IDataExportService
{
    private readonly ILocationService _locationService;

    public DataExportService(ILocationService locationService)
    {
        _locationService = locationService;
    }

    public async Task<Stream> ExportToJsonAsync(int userId)
    {
        var locations = await _locationService.GetAllLocationsAsync(userId);
        
        var exportData = new
        {
            locations = locations.Select(loc => new
            {
                name = loc.Name,
                tripName = loc.TripName,
                locationType = loc.LocationType,
                address = loc.Address,
                city = loc.City,
                state = loc.State,
                zipCode = loc.ZipCode,
                latitude = loc.Latitude,
                longitude = loc.Longitude,
                startDate = loc.StartDate.ToString("yyyy-MM-dd"),
                endDate = loc.EndDate?.ToString("yyyy-MM-dd"),
                rating = loc.Rating,
                comments = loc.Comments,
                tags = loc.Tags
            })
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(exportData, options);
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        stream.Position = 0;
        
        return stream;
    }

    public async Task<Stream> ExportToCsvAsync(int userId)
    {
        var locations = await _locationService.GetAllLocationsAsync(userId);
        var csv = new StringBuilder();
        
        // Add header
        csv.AppendLine("Location,Arrival,Departure,Comments,Address,Latitude,Longitude,Type,TripName");
        
        // Add data rows
        foreach (var loc in locations)
        {
            var fields = new[]
            {
                EscapeCsvField(loc.Name),
                loc.StartDate.ToString("yyyy-MM-dd"),
                loc.EndDate?.ToString("yyyy-MM-dd") ?? string.Empty,
                EscapeCsvField(loc.Comments),
                EscapeCsvField(loc.Address),
                loc.Latitude.ToString(CultureInfo.InvariantCulture),
                loc.Longitude.ToString(CultureInfo.InvariantCulture),
                EscapeCsvField(loc.LocationType),
                EscapeCsvField(loc.TripName ?? string.Empty)
            };
            
            csv.AppendLine(string.Join(",", fields));
        }
        
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv.ToString()));
        stream.Position = 0;
        
        return stream;
    }

    private string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
            return string.Empty;
        
        // If field contains comma, quote, or newline, wrap in quotes and escape quotes
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        
        return field;
    }
}
