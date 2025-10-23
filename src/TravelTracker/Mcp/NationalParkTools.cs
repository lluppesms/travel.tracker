using System.ComponentModel;
using ModelContextProtocol.Server;
using TravelTracker.Data.Models;
using TravelTracker.Services.Interfaces;

namespace TravelTracker.Mcp;

/// <summary>
/// MCP tools for accessing national park information
/// </summary>
[McpServerToolType]
public class NationalParkTools
{
    private readonly INationalParkService _nationalParkService;
    private readonly IAuthenticationService _authenticationService;

    public NationalParkTools(
        INationalParkService nationalParkService,
        IAuthenticationService authenticationService)
    {
        _nationalParkService = nationalParkService;
        _authenticationService = authenticationService;
    }

    /// <summary>
    /// Get all national parks
    /// </summary>
    [McpServerTool]
    [Description("Get a list of all US national parks in the database. No authentication required.")]
    public async Task<IEnumerable<NationalPark>> GetAllNationalParks()
    {
        return await _nationalParkService.GetAllParksAsync();
    }

    /// <summary>
    /// Get a national park by ID and state
    /// </summary>
    [McpServerTool]
    [Description("Get details of a specific national park by its ID and state code.")]
    public async Task<NationalPark?> GetNationalParkById(
        [Description("The unique identifier of the national park")] int parkId,
        [Description("Two-letter US state code where the park is located")] string state)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            throw new ArgumentException("State parameter is required");
        }

        return await _nationalParkService.GetParkByIdAsync(parkId, state);
    }

    /// <summary>
    /// Get national parks by state
    /// </summary>
    [McpServerTool]
    [Description("Get all national parks in a specific US state.")]
    public async Task<IEnumerable<NationalPark>> GetNationalParksByState(
        [Description("Two-letter US state code (e.g., 'CA', 'WY', 'UT')")] string state)
    {
        return await _nationalParkService.GetParksByStateAsync(state);
    }

    /// <summary>
    /// Get visited national parks
    /// </summary>
    [McpServerTool]
    [Description("Get all national parks that the authenticated user has visited. Requires authentication.")]
    public async Task<IEnumerable<NationalPark>> GetVisitedNationalParks()
    {
        var userId = _authenticationService.GetCurrentUserInternalId();
        if (userId == 0)
        {
            throw new UnauthorizedAccessException("User not authenticated");
        }

        return await _nationalParkService.GetVisitedParksAsync(userId);
    }
}
