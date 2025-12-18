namespace TravelTracker.Mcp;

/// <summary>
/// MCP tools for managing travel locations
/// </summary>
[AllowAnonymous]
[McpServerToolType]
public class LocationTools
{
    private readonly ILocationService _locationService;
    private readonly IAuthenticationService _authenticationService;

    public LocationTools(ILocationService locationService, IAuthenticationService authenticationService)
    {
        _locationService = locationService;
        _authenticationService = authenticationService;
    }

    /// <summary>
    /// Get all locations for the authenticated user
    /// </summary>
    [McpServerTool(Name = "get_all_locations")]
    [Description("Get all travel locations for the authenticated user. Returns a list of all visited locations including RV parks, national parks, and other travel destinations.")]
    public async Task<IEnumerable<Location>> GetAllLocations(
    [Description("The unique identifier of the user being queried")] int userId)
    {
        var (validatedUserId, errorMessage) = _authenticationService.ValidateUserAccess(userId);
        if (validatedUserId == 0) { throw new UnauthorizedAccessException(errorMessage); }

        return await _locationService.GetAllLocationsAsync(validatedUserId);
    }

    /// <summary>
    /// Get a specific location by ID
    /// </summary>
    [McpServerTool(Name = "get_location_by_id")]
    [Description("Get details of a specific location by its ID. Requires authentication and user must own the location.")]
    public async Task<Location?> GetLocationById(
    [Description("The unique identifier of the user being queried")] int userId,
    [Description("The unique identifier of the location")] int locationId)
    {
        var (validatedUserId, errorMessage) = _authenticationService.ValidateUserAccess(userId);
        if (validatedUserId == 0) { throw new UnauthorizedAccessException(errorMessage); }

        return await _locationService.GetLocationByIdAsync(locationId, validatedUserId);
    }

    /// <summary>
    /// Get locations by state
    /// </summary>
    [McpServerTool(Name = "get_locations_by_state")]
    [Description("Get all locations in a specific US state. Useful for viewing travel history in a particular state.")]
    public async Task<IEnumerable<Location>> GetLocationsByState(
    [Description("The unique identifier of the user being queried")] int userId,
    [Description("Two-letter US state code (e.g., 'CA', 'NY', 'WY')")] string state)
    {
        var (validatedUserId, errorMessage) = _authenticationService.ValidateUserAccess(userId);
        if (validatedUserId == 0) { throw new UnauthorizedAccessException(errorMessage); }

        return await _locationService.GetLocationsByStateAsync(validatedUserId, state);
    }

    /// <summary>
    /// Get locations by date range
    /// </summary>
    [McpServerTool(Name = "get_locations_by_date_range")]
    [Description("Get all locations visited within a specific date range. Useful for reviewing trips during a particular time period.")]
    public async Task<IEnumerable<Location>> GetLocationsByDateRange(
    [Description("The unique identifier of the user being queried")] int userId,
    [Description("Start date in ISO 8601 format (e.g., '2024-01-01')")] DateTime startDate,
    [Description("End date in ISO 8601 format (e.g., '2024-12-31')")] DateTime endDate)
    {
        var (validatedUserId, errorMessage) = _authenticationService.ValidateUserAccess(userId);
        if (validatedUserId == 0) { throw new UnauthorizedAccessException(errorMessage); }

        if (startDate > endDate)
        {
            throw new ArgumentException("Start date must be before end date");
        }

        return await _locationService.GetLocationsByDateRangeAsync(validatedUserId, startDate, endDate);
    }

    /// <summary>
    /// Get location count by state
    /// </summary>
    [McpServerTool(Name = "get_location_count_by_state")]
    [Description("Get a count of locations grouped by US state. Shows how many places have been visited in each state.")]
    public async Task<Dictionary<string, int>> GetLocationCountByState(
    [Description("The unique identifier of the user being queried")] int userId)
    {
        var (validatedUserId, errorMessage) = _authenticationService.ValidateUserAccess(userId);
        if (validatedUserId == 0) { throw new UnauthorizedAccessException(errorMessage); }

        return await _locationService.GetLocationsByStateCountAsync(validatedUserId);
    }

    ///// <summary>
    ///// Create a new location
    ///// </summary>
    //[McpServerTool]
    //[Description("Create a new travel location. Requires location name, type, address details, and optional rating/comments.")]
    //public async Task<Location?> CreateLocation(
    //    [Description("Name of the location")] string name,
    //    [Description("Type of location (e.g., 'RV Park', 'National Park', 'State Park')")] string locationType,
    //    [Description("Street address")] string address,
    //    [Description("City name")] string city,
    //    [Description("Two-letter US state code")] string state,
    //    [Description("ZIP/postal code")] string zipCode,
    //    [Description("Latitude coordinate")] double? latitude = null,
    //    [Description("Longitude coordinate")] double? longitude = null,
    //    [Description("Visit start date in ISO 8601 format")] DateTime? startDate = null,
    //    [Description("Visit end date in ISO 8601 format")] DateTime? endDate = null,
    //    [Description("Rating from 1-5 stars")] int? rating = null,
    //    [Description("Comments or notes about the location")] string? comments = null)
    //{
    //    var userId = _authenticationService.GetCurrentUserInternalId();
    //    if (userId == 0)
    //    {
    //        throw new UnauthorizedAccessException("User not authenticated");
    //    }

    //    var location = new Location
    //    {
    //        UserId = userId,
    //        Name = name,
    //        LocationType = locationType,
    //        Address = address,
    //        City = city,
    //        State = state,
    //        ZipCode = zipCode,
    //        Latitude = latitude ?? 0.0,
    //        Longitude = longitude ?? 0.0,
    //        StartDate = startDate ?? DateTime.UtcNow,
    //        EndDate = endDate,
    //        Rating = rating ?? 0,
    //        Comments = comments ?? string.Empty,
    //        CreatedDate = DateTime.UtcNow,
    //        ModifiedDate = DateTime.UtcNow
    //    };

    //    return await _locationService.CreateLocationAsync(location);
    //}

    ///// <summary>
    ///// Update an existing location
    ///// </summary>
    //[McpServerTool]
    //[Description("Update details of an existing location. User must own the location to update it.")]
    //public async Task<Location?> UpdateLocation(
    //    [Description("ID of the location to update")] int locationId,
    //    [Description("Updated name")] string? name = null,
    //    [Description("Updated location type")] string? locationType = null,
    //    [Description("Updated address")] string? address = null,
    //    [Description("Updated city")] string? city = null,
    //    [Description("Updated state code")] string? state = null,
    //    [Description("Updated ZIP code")] string? zipCode = null,
    //    [Description("Updated latitude")] double? latitude = null,
    //    [Description("Updated longitude")] double? longitude = null,
    //    [Description("Updated start date")] DateTime? startDate = null,
    //    [Description("Updated end date")] DateTime? endDate = null,
    //    [Description("Updated rating (1-5)")] int? rating = null,
    //    [Description("Updated comments")] string? comments = null)
    //{
    //    var userId = _authenticationService.GetCurrentUserInternalId();
    //    if (userId == 0)
    //    {
    //        throw new UnauthorizedAccessException("User not authenticated");
    //    }

    //    var existingLocation = await _locationService.GetLocationByIdAsync(locationId, userId);
    //    if (existingLocation == null)
    //    {
    //        throw new InvalidOperationException($"Location with ID {locationId} not found");
    //    }

    //    // Update only provided fields
    //    if (name != null) existingLocation.Name = name;
    //    if (locationType != null) existingLocation.LocationType = locationType;
    //    if (address != null) existingLocation.Address = address;
    //    if (city != null) existingLocation.City = city;
    //    if (state != null) existingLocation.State = state;
    //    if (zipCode != null) existingLocation.ZipCode = zipCode;
    //    if (latitude.HasValue) existingLocation.Latitude = latitude.Value;
    //    if (longitude.HasValue) existingLocation.Longitude = longitude.Value;
    //    if (startDate.HasValue) existingLocation.StartDate = startDate.Value;
    //    if (endDate.HasValue) existingLocation.EndDate = endDate;
    //    if (rating.HasValue) existingLocation.Rating = rating.Value;
    //    if (comments != null) existingLocation.Comments = comments;

    //    existingLocation.ModifiedDate = DateTime.UtcNow;

    //    return await _locationService.UpdateLocationAsync(existingLocation);
    //}

    ///// <summary>
    ///// Delete a location
    ///// </summary>
    //[McpServerTool]
    //[Description("Delete a location by its ID. User must own the location to delete it.")]
    //public async Task<bool> DeleteLocation(
    //    [Description("ID of the location to delete")] int locationId)
    //{
    //    var userId = _authenticationService.GetCurrentUserInternalId();
    //    if (userId == 0)
    //    {
    //        throw new UnauthorizedAccessException("User not authenticated");
    //    }

    //    var existingLocation = await _locationService.GetLocationByIdAsync(locationId, userId);
    //    if (existingLocation == null)
    //    {
    //        throw new InvalidOperationException($"Location with ID {locationId} not found");
    //    }

    //    await _locationService.DeleteLocationAsync(locationId, userId);
    //    return true;
    //}
}
