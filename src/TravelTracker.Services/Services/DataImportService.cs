using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions; // added for zip code parsing
using TravelTracker.Data.Models;
using TravelTracker.Data.Repositories;
using TravelTracker.Services.Interfaces;

namespace TravelTracker.Services.Services;

public class DataImportService : IDataImportService
{
    private readonly ILocationService _locationService;
    private readonly ILocationTypeRepository _locationTypeRepository;
    private readonly INationalParkRepository _nationalParkRepository;

    public DataImportService(
        ILocationService locationService,
        ILocationTypeRepository locationTypeRepository,
        INationalParkRepository nationalParkRepository)
    {
        _locationService = locationService;
        _locationTypeRepository = locationTypeRepository;
        _nationalParkRepository = nationalParkRepository;
    }

    public async Task<ImportResult> ImportFromJsonAsync(Stream jsonStream, int userId)
    {
        var result = new ImportResult();

        try
        {
            using var reader = new StreamReader(jsonStream);
            var jsonContent = await reader.ReadToEndAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var uploadData = JsonSerializer.Deserialize<LocationUploadData>(jsonContent, options);

            if (uploadData?.Locations == null)
            {
                result.Errors.Add("Invalid JSON structure. Missing 'locations' array.");
                return result;
            }

            result.TotalRecords = uploadData.Locations.Count;

            foreach (var locData in uploadData.Locations)
            {
                try
                {
                    var location = await MapJsonToLocationAsync(locData, userId);
                    await _locationService.CreateLocationAsync(location);
                    result.ImportedRecords++;
                }
                catch (Exception ex)
                {
                    result.FailedRecords++;
                    result.Errors.Add($"Failed to import location '{locData.Name}': {ex.Message}");
                }
            }

            result.Success = result.ImportedRecords > 0;
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Import failed: {ex.Message}");
        }

        return result;
    }

    public async Task<ImportResult> ImportFromCsvAsync(Stream csvStream, int userId)
    {
        var result = new ImportResult();

        try
        {
            using var reader = new StreamReader(csvStream);

            // Read header
            var header = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(header))
            {
                result.Errors.Add("CSV file is empty or missing header row.");
                return result;
            }

            // Validate header format
            var expectedHeader = "Location,Arrival,Departure,Comments,Address,Latitude,Longitude,Type";
            if (!header.Trim().Equals(expectedHeader, StringComparison.OrdinalIgnoreCase))
            {
                result.Errors.Add($"Invalid CSV header. Expected: {expectedHeader}");
                return result;
            }

            int lineNumber = 1;
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                lineNumber++;
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                try
                {
                    var location = await ParseCsvLineAsync(line, userId, lineNumber);
                    if (location != null)
                    {
                        await _locationService.CreateLocationAsync(location);
                        result.ImportedRecords++;
                    }
                    result.TotalRecords++;
                }
                catch (Exception ex)
                {
                    result.FailedRecords++;
                    result.Errors.Add($"Line {lineNumber}: {ex.Message}");
                }
            }

            result.Success = result.ImportedRecords > 0;
        }
        catch (Exception ex)
        {
            result.Errors.Add($"CSV import failed: {ex.Message}");
        }

        return result;
    }

    public async Task<ValidationResult> ValidateJsonAsync(Stream jsonStream)
    {
        var result = new ValidationResult { IsValid = true };

        try
        {
            using var reader = new StreamReader(jsonStream);
            var jsonContent = await reader.ReadToEndAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var uploadData = JsonSerializer.Deserialize<LocationUploadData>(jsonContent, options);

            if (uploadData?.Locations == null)
            {
                result.IsValid = false;
                result.Errors.Add("Invalid JSON structure. Missing 'locations' array.");
                return result;
            }

            result.RecordCount = uploadData.Locations.Count;
            result.ValidationMessages.Add($"Found {result.RecordCount} locations in JSON file.");

            int validLocations = 0;
            foreach (var loc in uploadData.Locations)
            {
                if (!string.IsNullOrWhiteSpace(loc.Name) && !string.IsNullOrWhiteSpace(loc.State))
                {
                    validLocations++;
                }
            }

            if (validLocations == 0)
            {
                result.IsValid = false;
                result.Errors.Add("No valid locations found. Each location must have at least a name and state.");
            }
            else
            {
                result.ValidationMessages.Add($"{validLocations} locations are valid and ready for import.");
            }
        }
        catch (JsonException ex)
        {
            result.IsValid = false;
            result.Errors.Add($"JSON parsing error: {ex.Message}");
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.Errors.Add($"Validation failed: {ex.Message}");
        }

        return result;
    }

    public async Task<ValidationResult> ValidateCsvAsync(Stream csvStream)
    {
        var result = new ValidationResult { IsValid = true };

        try
        {
            using var reader = new StreamReader(csvStream);

            var header = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(header))
            {
                result.IsValid = false;
                result.Errors.Add("CSV file is empty or missing header row.");
                return result;
            }

            var expectedHeader = "Location,Arrival,Departure,Comments,Address,Latitude,Longitude,Type";
            if (!header.Trim().Equals(expectedHeader, StringComparison.OrdinalIgnoreCase))
            {
                result.IsValid = false;
                result.Errors.Add($"Invalid CSV header. Expected: {expectedHeader}");
                return result;
            }

            result.ValidationMessages.Add("CSV header is valid.");

            int lineCount = 0;
            int validLines = 0;
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                lineCount++;
                try
                {
                    var fields = ParseCsvFields(line);
                    if (fields.Length >= 8 && !string.IsNullOrWhiteSpace(fields[1]))
                    {
                        validLines++;
                    }
                }
                catch
                {
                    // Invalid line, continue counting
                }
            }

            result.RecordCount = lineCount;
            result.ValidationMessages.Add($"Found {lineCount} data rows in CSV file.");

            if (validLines == 0)
            {
                result.IsValid = false;
                result.Errors.Add("No valid data rows found in CSV file.");
            }
            else
            {
                result.ValidationMessages.Add($"{validLines} rows appear valid and ready for import.");
            }
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.Errors.Add($"CSV validation failed: {ex.Message}");
        }

        return result;
    }

    private async Task<Location> MapJsonToLocationAsync(LocationData locData, int userId)
    {
        var locationName = locData.Name ?? "Unknown";
        var locationType = locData.LocationType ?? "Other";
        
        // Validate location type
        locationType = await ValidateAndNormalizeLocationTypeAsync(locationType, locationName);
        
        return new Location
        {
            UserId = userId,
            Name = locationName,
            LocationType = locationType,
            Address = locData.Address ?? string.Empty,
            City = locData.City ?? string.Empty,
            State = locData.State ?? string.Empty,
            ZipCode = locData.ZipCode ?? string.Empty,
            Latitude = locData.Latitude,
            Longitude = locData.Longitude,
            StartDate = locData.StartDate,
            EndDate = locData.EndDate,
            Rating = locData.Rating,
            Comments = locData.Comments ?? string.Empty,
            Tags = locData.Tags ?? new List<string>()
        };
    }

    private async Task<Location?> ParseCsvLineAsync(string line, int userId, int lineNumber)
    {
        var fields = ParseCsvFields(line);

        if (fields.Length < 8)
        {
            throw new FormatException($"Invalid CSV format. Expected 8 fields, found {fields.Length}.");
        }

        // CSV Format: Location,Arrival,Departure,Comments,Address,Latitude,Longitude,Type
        var locationName = fields[0].Trim();
        var arrival = fields[1].Trim();
        var departure = fields[2].Trim();
        var comments = fields[3].Trim();
        var address = fields[4].Trim();
        var latitudeStr = fields[5].Trim();
        var longitudeStr = fields[6].Trim();
        var rowType = fields[7].Trim();

        if (string.IsNullOrWhiteSpace(locationName))
        {
            throw new FormatException("Location name is required.");
        }

        // Parse coordinates
        if (!double.TryParse(latitudeStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double latitude))
        {
            throw new FormatException($"Invalid latitude value: {latitudeStr}");
        }

        if (!double.TryParse(longitudeStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double longitude))
        {
            throw new FormatException($"Invalid longitude value: {longitudeStr}");
        }

        // Parse dates
        DateTime? startDate = null;
        DateTime? endDate = null;

        if (!string.IsNullOrWhiteSpace(arrival) && DateTime.TryParse(arrival, out DateTime parsedStart))
        {
            startDate = parsedStart;
        }

        if (!string.IsNullOrWhiteSpace(departure) && DateTime.TryParse(departure, out DateTime parsedEnd))
        {
            endDate = parsedEnd;
        }

        // Extract state from address (try to find 2-letter state code)
        string state = ExtractStateFromAddress(address);
        string city = ExtractCityFromAddress(address);
        string zip = ExtractZipCodeFromAddress(address); // new zip extraction

        // Validate location type
        var locationType = string.IsNullOrWhiteSpace(rowType) ? "Other" : rowType;
        locationType = await ValidateAndNormalizeLocationTypeAsync(locationType, locationName);

        return new Location
        {
            UserId = userId,
            Name = locationName,
            LocationType = locationType,
            Address = address,
            City = city,
            State = state,
            ZipCode = zip,
            Latitude = latitude,
            Longitude = longitude,
            StartDate = startDate ?? DateTime.UtcNow,
            EndDate = endDate,
            Rating = 5, // CSV doesn't specify rating
            Comments = comments,
            Tags = new List<string>()
        };
    }

    private string[] ParseCsvFields(string line)
    {
        var fields = new List<string>();
        var currentField = string.Empty;
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(currentField);
                currentField = string.Empty;
            }
            else
            {
                currentField += c;
            }
        }

        fields.Add(currentField); // Add the last field
        return fields.ToArray();
    }

    private string ExtractStateFromAddress(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return string.Empty;

        // Common US state abbreviations
        var stateAbbreviations = new[]
        {
            "AL", "AK", "AZ", "AR", "CA", "CO", "CT", "DE", "FL", "GA",
            "HI", "ID", "IL", "IN", "IA", "KS", "KY", "LA", "ME", "MD",
            "MA", "MI", "MN", "MS", "MO", "MT", "NE", "NV", "NH", "NJ",
            "NM", "NY", "NC", "ND", "OH", "OK", "OR", "PA", "RI", "SC",
            "SD", "TN", "TX", "UT", "VT", "VA", "WA", "WV", "WI", "WY"
        };

        // Look for state abbreviation pattern (2 uppercase letters preceded by space or comma)
        var parts = address.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts.Reverse())
        {
            var trimmed = part.Trim().ToUpper();
            if (trimmed.Length == 2 && stateAbbreviations.Contains(trimmed))
            {
                return trimmed;
            }
        }

        return string.Empty;
    }

    private string ExtractZipCodeFromAddress(string address)
    {
        if (string.IsNullOrWhiteSpace(address)) return string.Empty;

        // Match US ZIP code 5 digits optionally followed by -4 extension
        var match = Regex.Match(address, @"\b\d{5}(?:-\d{4})?\b");
        return match.Success ? match.Value : string.Empty;
    }

    private string ExtractCityFromAddress(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return string.Empty;

        // Try to extract city (usually before the state)
        var parts = address.Split(',');
        if (parts.Length >= 2)
        {
            return parts[parts.Length - 2].Trim();
        }
        else if (parts.Length == 1)
        {
            // Try space-separated
            var spaceParts = address.Split(' ');
            if (spaceParts.Length > 2)
            {
                return string.Join(" ", spaceParts.Take(spaceParts.Length - 2));
            }
        }

        return string.Empty;
    }

    private async Task<string> ValidateAndNormalizeLocationTypeAsync(string locationType, string locationName)
    {
        if (string.IsNullOrWhiteSpace(locationType))
        {
            throw new ArgumentException("Location type is required.");
        }

        var normalizedType = locationType.Trim();
        var validType = await _locationTypeRepository.GetByNameAsync(normalizedType);
        
        if (validType == null)
        {
            var validTypes = await _locationTypeRepository.GetAllAsync();
            var validTypeNames = string.Join(", ", validTypes.Select(t => t.Name));
            throw new ArgumentException($"Invalid location type '{normalizedType}'. Valid types are: {validTypeNames}");
        }

        // Special validation for National Park type
        if (normalizedType.Equals("National Park", StringComparison.OrdinalIgnoreCase))
        {
            var allParks = await _nationalParkRepository.GetAllAsync();
            var matchingPark = allParks.FirstOrDefault(park =>
                park.Name.Equals(locationName, StringComparison.OrdinalIgnoreCase) ||
                park.Name.Contains(locationName, StringComparison.OrdinalIgnoreCase) ||
                locationName.Contains(park.Name, StringComparison.OrdinalIgnoreCase));

            if (matchingPark == null)
            {
                throw new ArgumentException($"National Park '{locationName}' is not found in the National Parks database. Please verify the park name.");
            }
        }

        return normalizedType;
    }

    private class LocationUploadData
    {
        public List<LocationData>? Locations { get; set; }
    }

    private class LocationData
    {
        public string? Name { get; set; }
        public string? LocationType { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int Rating { get; set; }
        public string? Comments { get; set; }
        public List<string>? Tags { get; set; }
    }
}
