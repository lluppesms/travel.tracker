namespace TravelTracker.Mcp;

/// <summary>
/// MCP tools for accessing national park information
/// </summary>
[AllowAnonymous]
[McpServerToolType]
public class NationalParkTools
{
    private readonly INationalParkService _nationalParkService;
    private readonly IAuthenticationService _authenticationService;

    public NationalParkTools(INationalParkService nationalParkService, IAuthenticationService authenticationService)
    {
        _nationalParkService = nationalParkService;
        _authenticationService = authenticationService;
    }

    /// <summary>
    /// Get all national parks
    /// </summary>
    [McpServerTool(Name = "get_all_national_parks")]
    [Description("Get a list of all US national parks in the database. No authentication required.")]
    public async Task<IEnumerable<NationalPark>> GetAllNationalParks()
    {
        return await _nationalParkService.GetAllParksAsync();
    }

    /// <summary>
    /// Get a national park by ID and state
    /// </summary>
    [McpServerTool(Name = "get_national_park_by_id")]
    [Description("Get details of a specific national park by its ID and state code.")]
    public async Task<NationalPark?> GetNationalParkById(
    [Description("The unique identifier of the national park")] int parkId)
    {
        return await _nationalParkService.GetParkByIdAsync(parkId);
    }

    /// <summary>
    /// Get national parks by state
    /// </summary>
    [McpServerTool(Name = "get_national_parks_by_state")]
    [Description("Get all national parks in a specific US state.")]
    public async Task<IEnumerable<NationalPark>> GetNationalParksByState(
    [Description("Two-letter US state code (e.g., 'CA', 'WY', 'UT')")] string state)
    {
        return await _nationalParkService.GetParksByStateAsync(state);
    }

    /// <summary>
    /// Get visited national parks
    /// </summary>
    [McpServerTool(Name = "get_visited_national_parks")]
    [Description("Get all national parks that the authenticated user has visited. Requires authentication.")]
    public async Task<IEnumerable<NationalPark>> GetVisitedNationalParks(
    [Description("The unique identifier of the user being queried")] int userId)
    {
        var (validatedUserId, errorMessage) = _authenticationService.ValidateUserAccess(userId);
        if (validatedUserId == 0) { throw new UnauthorizedAccessException(errorMessage); }

        return await _nationalParkService.GetVisitedParksAsync(validatedUserId);
    }
}
