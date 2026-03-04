using System.Text.Json;

namespace TravelTracker.Services.Services;

public class LocationLookupService : ILocationLookupService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LocationLookupService> _logger;

    // Always available – uses free public APIs, no Azure key required
    public bool IsConfigured => true;

    public LocationLookupService(HttpClient httpClient, ILogger<LocationLookupService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<LocationLookupResult> LookupLocationAsync(string name, string address, string city, string state, string zipCode)
    {
        try
        {
            // Step 1: Use Nominatim to resolve the address from name + city + state
            var result = await LookupAddressViaNominatimAsync(name, city, state);
            if (result == null)
            {
                return new LocationLookupResult
                {
                    Success = false,
                    ErrorMessage = $"Could not find '{name}' in {city}, {state}. Try providing more detail."
                };
            }

            // Step 2: Use Photon to get precise lat/lon from the resolved address
            var coords = await LookupCoordinatesViaPhotonAsync(
                result.Address, result.City, result.State, result.ZipCode);

            if (coords.HasValue)
            {
                result.Latitude = coords.Value.lat;
                result.Longitude = coords.Value.lon;
            }
            // If Photon fails, Nominatim's own lat/lon (already in result) is used as fallback

            result.Success = true;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error looking up location '{Name}'", name);
            return new LocationLookupResult { Success = false, ErrorMessage = $"Lookup failed: {ex.Message}" };
        }
    }

    private async Task<LocationLookupResult?> LookupAddressViaNominatimAsync(string name, string city, string state)
    {
        try
        {
            var queryParts = new[] { name, city, state }.Where(s => !string.IsNullOrWhiteSpace(s));
            var query = Uri.EscapeDataString(string.Join(", ", queryParts));
            var url = $"https://nominatim.openstreetmap.org/search?q={query}&format=json&addressdetails=1&limit=1&countrycodes=us";

            _logger.LogInformation("Nominatim request: {Url}", url);
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("Nominatim response: {Content}", content[..Math.Min(500, content.Length)]);

            using var doc = JsonDocument.Parse(content);
            var results = doc.RootElement;

            if (results.GetArrayLength() == 0)
            {
                _logger.LogInformation("Nominatim returned no results for '{Name}' in {City}, {State}", name, city, state);
                return null;
            }

            var first = results[0];
            var addressEl = first.GetProperty("address");

            var houseNumber = GetJsonString(addressEl, "house_number");
            var road = GetJsonString(addressEl, "road", "pedestrian", "footway", "path", "amenity", "tourism", "leisure");
            var streetAddress = string.IsNullOrEmpty(houseNumber) ? road : $"{houseNumber} {road}";

            var resultCity = GetJsonString(addressEl, "city", "town", "village", "municipality", "hamlet");
            var resultState = GetJsonString(addressEl, "state");
            var postcode = GetJsonString(addressEl, "postcode");

            // Nominatim also returns lat/lon for the found place (used as fallback if Photon fails)
            double.TryParse(GetTopLevelString(first, "lat"), NumberStyles.Float, CultureInfo.InvariantCulture, out var lat);
            double.TryParse(GetTopLevelString(first, "lon"), NumberStyles.Float, CultureInfo.InvariantCulture, out var lon);

            return new LocationLookupResult
            {
                Address = streetAddress.Trim(),
                City = resultCity,
                State = AbbreviateState(resultState),
                ZipCode = postcode?.Split('-')[0] ?? string.Empty,   // keep just the 5-digit part
                Latitude = lat,
                Longitude = lon,
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Nominatim lookup failed for '{Name}' in {City}, {State}", name, city, state);
            return null;
        }
    }

    private async Task<(double lat, double lon)?> LookupCoordinatesViaPhotonAsync(
        string address, string city, string state, string zipCode)
    {
        try
        {
            var queryParts = new[] { address, city, state, zipCode }.Where(s => !string.IsNullOrWhiteSpace(s));
            var query = Uri.EscapeDataString(string.Join(" ", queryParts));
            var url = $"https://photon.komoot.io/api?q={query}&limit=1";

            _logger.LogInformation("Photon request: {Url}", url);
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("Photon response: {Content}", content[..Math.Min(500, content.Length)]);

            using var doc = JsonDocument.Parse(content);
            if (!doc.RootElement.TryGetProperty("features", out var features) ||
                features.GetArrayLength() == 0)
            {
                _logger.LogInformation("Photon returned no features for address '{Address}'", address);
                return null;
            }

            // GeoJSON coordinates are [longitude, latitude]
            var coords = features[0].GetProperty("geometry").GetProperty("coordinates");
            var lon = coords[0].GetDouble();
            var lat = coords[1].GetDouble();
            return (lat, lon);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Photon lookup failed for address '{Address}'", address);
            return null;
        }
    }

    // Returns the first non-empty string value among the given property names in a JSON element
    private static string GetJsonString(JsonElement element, params string[] propertyNames)
    {
        foreach (var prop in propertyNames)
        {
            if (element.TryGetProperty(prop, out var val) &&
                val.ValueKind == JsonValueKind.String &&
                !string.IsNullOrEmpty(val.GetString()))
            {
                return val.GetString()!;
            }
        }
        return string.Empty;
    }

    private static string GetTopLevelString(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var val) && val.ValueKind == JsonValueKind.String)
            return val.GetString() ?? string.Empty;
        return string.Empty;
    }

    // Convert full US state name to 2-letter postal abbreviation
    private static string AbbreviateState(string stateName)
    {
        if (string.IsNullOrEmpty(stateName))
            return stateName;
        if (stateName.Length == 2)
            return stateName.ToUpperInvariant();
        return StateAbbreviations.TryGetValue(stateName, out var abbr) ? abbr : stateName;
    }

    private static readonly Dictionary<string, string> StateAbbreviations = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Alabama", "AL" }, { "Alaska", "AK" }, { "Arizona", "AZ" }, { "Arkansas", "AR" },
        { "California", "CA" }, { "Colorado", "CO" }, { "Connecticut", "CT" }, { "Delaware", "DE" },
        { "Florida", "FL" }, { "Georgia", "GA" }, { "Hawaii", "HI" }, { "Idaho", "ID" },
        { "Illinois", "IL" }, { "Indiana", "IN" }, { "Iowa", "IA" }, { "Kansas", "KS" },
        { "Kentucky", "KY" }, { "Louisiana", "LA" }, { "Maine", "ME" }, { "Maryland", "MD" },
        { "Massachusetts", "MA" }, { "Michigan", "MI" }, { "Minnesota", "MN" }, { "Mississippi", "MS" },
        { "Missouri", "MO" }, { "Montana", "MT" }, { "Nebraska", "NE" }, { "Nevada", "NV" },
        { "New Hampshire", "NH" }, { "New Jersey", "NJ" }, { "New Mexico", "NM" }, { "New York", "NY" },
        { "North Carolina", "NC" }, { "North Dakota", "ND" }, { "Ohio", "OH" }, { "Oklahoma", "OK" },
        { "Oregon", "OR" }, { "Pennsylvania", "PA" }, { "Rhode Island", "RI" }, { "South Carolina", "SC" },
        { "South Dakota", "SD" }, { "Tennessee", "TN" }, { "Texas", "TX" }, { "Utah", "UT" },
        { "Vermont", "VT" }, { "Virginia", "VA" }, { "Washington", "WA" }, { "West Virginia", "WV" },
        { "Wisconsin", "WI" }, { "Wyoming", "WY" }, { "District of Columbia", "DC" }
    };
}
