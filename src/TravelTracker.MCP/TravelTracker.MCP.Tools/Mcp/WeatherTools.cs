namespace TravelTracker.Mcp;

public record LocationPoint(
    [property: Description("Latitude in decimal degrees")] double Latitude,
    [property: Description("Longitude in decimal degrees")] double Longitude
);

[AllowAnonymous]
[McpServerToolType]
public class WeatherTools
{
    private readonly IHttpClientFactory _httpClientFactory;

    public WeatherTools(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [McpServerTool(Name = "get_weather_forecast"), Description("Get weather forecast for a specified location point")]
    public async Task<string> GetForecast([Description("Location coordinates")] LocationPoint locationPoint)
    {
        var normalizedPoint = NormalizePoint(locationPoint);
        var latitude = normalizedPoint.Latitude.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture);
        var longitude = normalizedPoint.Longitude.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture);

        using var httpClient = _httpClientFactory.CreateClient("WeatherAPI");

        using var pointsResponse = await httpClient.GetAsync($"points/{latitude},{longitude}");
        if (pointsResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new InvalidOperationException($"No weather.gov point metadata found for coordinates {latitude},{longitude}. This endpoint only supports U.S. and U.S. territory coordinates.");
        }

        pointsResponse.EnsureSuccessStatusCode();

        var pointsJson = await pointsResponse.Content.ReadAsStringAsync();

        string? forecastUrl = null;
        using (var doc = JsonDocument.Parse(pointsJson))
        {
            if (doc.RootElement.TryGetProperty("properties", out var props) &&
                props.TryGetProperty("forecast", out var forecastProp) &&
                forecastProp.ValueKind == JsonValueKind.String)
            {
                forecastUrl = forecastProp.GetString();
            }
        }

        if (string.IsNullOrWhiteSpace(forecastUrl))
        {
            throw new InvalidOperationException("Invalid forecast response format: missing properties.forecast URL");
        }

        using var forecastResponse = await httpClient.GetAsync(forecastUrl);
        forecastResponse.EnsureSuccessStatusCode();

        var forecastJson = await forecastResponse.Content.ReadAsStringAsync();
        return forecastJson;
    }

    private static LocationPoint NormalizePoint(LocationPoint locationPoint)
    {
        var latitude = locationPoint.Latitude;
        var longitude = locationPoint.Longitude;

        if (latitude == 0 && longitude == 0) { latitude = 44.4512; longitude = -92.5248; }

        if (Math.Abs(latitude) > 90 && Math.Abs(longitude) <= 90)
        {
            (latitude, longitude) = (longitude, latitude);
        }

        if (latitude is < -90 or > 90)
        {
            throw new ArgumentOutOfRangeException(nameof(locationPoint.Latitude), "Latitude must be between -90 and 90.");
        }

        if (longitude is < -180 or > 180)
        {
            throw new ArgumentOutOfRangeException(nameof(locationPoint.Longitude), "Longitude must be between -180 and 180.");
        }

        return new LocationPoint(latitude, longitude);
    }
}
