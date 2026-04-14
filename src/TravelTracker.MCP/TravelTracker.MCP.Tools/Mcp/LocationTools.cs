using System.Net;
using System.Net.Http.Json;

namespace TravelTracker.Mcp;

[AllowAnonymous]
[McpServerToolType]
public class LocationTools
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAuthenticationService _authenticationService;
    private readonly string _apiKey;

    public LocationTools(IHttpClientFactory httpClientFactory, IAuthenticationService authenticationService, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _authenticationService = authenticationService;

        _apiKey = configuration["ApiKey"] ?? string.Empty;
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new InvalidOperationException("Missing required ApiKey configuration for MCP location tools.");
        }
    }

    [McpServerTool(Name = "get_all_locations")]
    [Description("Get all travel locations for the authenticated user. Returns a list of all visited locations including RV parks, national parks, and other travel destinations.")]
    public async Task<IEnumerable<Dictionary<string, object?>>> GetAllLocations(
        [Description("The unique identifier of the user being queried")] int userId)
    {
        var (validatedUserId, errorMessage) = _authenticationService.ValidateUserAccess(userId);
        if (validatedUserId == 0)
        {
            throw new UnauthorizedAccessException(errorMessage);
        }

        return await SendGetAsync<List<Dictionary<string, object?>>>($"{validatedUserId}");
    }

    [McpServerTool(Name = "get_location_by_id")]
    [Description("Get details of a specific location by its ID. Requires authentication and user must own the location.")]
    public async Task<Dictionary<string, object?>?> GetLocationById(
        [Description("The unique identifier of the user being queried")] int userId,
        [Description("The unique identifier of the location")] int locationId)
    {
        var (validatedUserId, errorMessage) = _authenticationService.ValidateUserAccess(userId);
        if (validatedUserId == 0)
        {
            throw new UnauthorizedAccessException(errorMessage);
        }

        using var client = _httpClientFactory.CreateClient("TravelTrackerLocationsApi");
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{validatedUserId}/{locationId}");
        request.Headers.Add("X-API-Key", _apiKey);

        using var response = await client.SendAsync(request);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            throw await CreateRequestException(response, request.RequestUri?.ToString() ?? string.Empty);
        }

        return await response.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
    }

    [McpServerTool(Name = "get_locations_by_state")]
    [Description("Get all locations in a specific US state. Useful for viewing travel history in a particular state.")]
    public async Task<IEnumerable<Dictionary<string, object?>>> GetLocationsByState(
        [Description("The unique identifier of the user being queried")] int userId,
        [Description("Two-letter US state code (e.g., 'CA', 'NY', 'WY')")] string state)
    {
        var (validatedUserId, errorMessage) = _authenticationService.ValidateUserAccess(userId);
        if (validatedUserId == 0)
        {
            throw new UnauthorizedAccessException(errorMessage);
        }

        return await SendGetAsync<List<Dictionary<string, object?>>>($"by-state/{validatedUserId}/{Uri.EscapeDataString(state)}");
    }

    [McpServerTool(Name = "get_locations_by_date_range")]
    [Description("Get all locations visited within a specific date range. Useful for reviewing trips during a particular time period.")]
    public async Task<IEnumerable<Dictionary<string, object?>>> GetLocationsByDateRange(
        [Description("The unique identifier of the user being queried")] int userId,
        [Description("Start date in ISO 8601 format (e.g., '2024-01-01')")] DateTime startDate,
        [Description("End date in ISO 8601 format (e.g., '2024-12-31')")] DateTime endDate)
    {
        var (validatedUserId, errorMessage) = _authenticationService.ValidateUserAccess(userId);
        if (validatedUserId == 0)
        {
            throw new UnauthorizedAccessException(errorMessage);
        }

        if (startDate > endDate)
        {
            throw new ArgumentException("Start date must be before end date");
        }

        var start = Uri.EscapeDataString(startDate.ToString("O"));
        var end = Uri.EscapeDataString(endDate.ToString("O"));
        return await SendGetAsync<List<Dictionary<string, object?>>>($"by-date-range/{validatedUserId}?startDate={start}&endDate={end}");
    }

    [McpServerTool(Name = "get_location_count_by_state")]
    [Description("Get a count of locations grouped by US state. Shows how many places have been visited in each state.")]
    public async Task<Dictionary<string, int>> GetLocationCountByState(
        [Description("The unique identifier of the user being queried")] int userId)
    {
        var (validatedUserId, errorMessage) = _authenticationService.ValidateUserAccess(userId);
        if (validatedUserId == 0)
        {
            throw new UnauthorizedAccessException(errorMessage);
        }

        return await SendGetAsync<Dictionary<string, int>>($"count-by-state/{validatedUserId}");
    }

    private async Task<T> SendGetAsync<T>(string relativeUri)
    {
        using var client = _httpClientFactory.CreateClient("TravelTrackerLocationsApi");
        using var request = new HttpRequestMessage(HttpMethod.Get, relativeUri);
        request.Headers.Add("X-API-Key", _apiKey);

        using var response = await client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            throw await CreateRequestException(response, request.RequestUri?.ToString() ?? relativeUri);
        }

        var payload = await response.Content.ReadFromJsonAsync<T>();
        if (payload is null)
        {
            throw new InvalidOperationException($"Locations API returned an empty response for route '{relativeUri}'.");
        }

        return payload;
    }

    private static async Task<Exception> CreateRequestException(HttpResponseMessage response, string route)
    {
        var errorBody = await response.Content.ReadAsStringAsync();
        var details = string.IsNullOrWhiteSpace(errorBody)
            ? response.ReasonPhrase
            : errorBody;

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return new UnauthorizedAccessException($"Location API unauthorized for route '{route}'. Details: {details}");
        }

        return new HttpRequestException(
            $"Location API request failed with status {(int)response.StatusCode} for route '{route}'. Details: {details}");
    }
}
