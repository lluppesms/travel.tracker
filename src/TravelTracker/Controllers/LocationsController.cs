using Microsoft.AspNetCore.Mvc;
using RouteAttribute = Microsoft.AspNetCore.Mvc.RouteAttribute;

namespace TravelTracker.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/[controller]")]
public class LocationsController(ILocationService locationService, IAuthenticationService authenticationService, ILogger<LocationsController> logger) : ControllerBase
{
    private readonly ILocationService _locationService = locationService;
    private readonly IAuthenticationService _authenticationService = authenticationService;
    private readonly ILogger<LocationsController> _logger = logger;

    /// <summary>
    /// Get all locations for the authenticated user
    /// </summary>
    [HttpGet("{userid}")]
    public async Task<ActionResult<IEnumerable<Location>>> GetAllLocations(int userId)
    {
        var (validatedUserId, errorMessage) = _authenticationService.ValidateUserAccess(userId);
        if (validatedUserId == 0) { return Unauthorized(new { message = errorMessage }); }

        var locations = await _locationService.GetAllLocationsAsync(validatedUserId);
        return Ok(locations);
    }

    /// <summary>
    /// Get a specific location by ID
    /// </summary>
    [HttpGet("{userid}/{id}")]
    public async Task<ActionResult<Location>> GetLocationById(int id, int userId)
    {
        var (validatedUserId, errorMessage) = _authenticationService.ValidateUserAccess(userId);
        if (validatedUserId == 0) { return Unauthorized(new { message = errorMessage }); }

        var location = await _locationService.GetLocationByIdAsync(id, validatedUserId);
        if (location == null)
        {
            return NotFound(new { message = $"Location with ID {id} not found" });
        }

        return Ok(location);
    }

    /// <summary>
    /// Get locations by state
    /// </summary>
    [HttpGet("by-state/{userid}/{state}")]
    public async Task<ActionResult<IEnumerable<Location>>> GetLocationsByState(string state, int userId)
    {
        var (validatedUserId, errorMessage) = _authenticationService.ValidateUserAccess(userId);
        if (validatedUserId == 0) { return Unauthorized(new { message = errorMessage }); }

        var locations = await _locationService.GetLocationsByStateAsync(validatedUserId, state);
        return Ok(locations);
    }

    /// <summary>
    /// Get locations by date range
    /// </summary>
    [HttpGet("by-date-range/{userid}")]
    public async Task<ActionResult<IEnumerable<Location>>> GetLocationsByDateRange(int userId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        var (validatedUserId, errorMessage) = _authenticationService.ValidateUserAccess(userId);
        if (validatedUserId == 0) { return Unauthorized(new { message = errorMessage }); }

        if (startDate > endDate)
        {
            return BadRequest(new { message = "Start date must be before end date" });
        }

        var locations = await _locationService.GetLocationsByDateRangeAsync(validatedUserId, startDate, endDate);
        return Ok(locations);
    }

    /// <summary>
    /// Get location count by state
    /// </summary>
    [HttpGet("count-by-state/{userid}")]
    public async Task<ActionResult<Dictionary<string, int>>> GetLocationsByStateCount(int userId)
    {
        var (validatedUserId, errorMessage) = _authenticationService.ValidateUserAccess(userId);
        if (validatedUserId == 0) { return Unauthorized(new { message = errorMessage }); }

        var stateCounts = await _locationService.GetLocationsByStateCountAsync(validatedUserId);
        return Ok(stateCounts);
    }

    ///// <summary>
    ///// Create a new location
    ///// </summary>
    //[HttpPost]
    //public async Task<ActionResult<Location>> CreateLocation([FromBody] Location location)
    //{
    //    var userId = _authenticationService.GetCurrentUserInternalId();
    //    if (userId == 0)
    //    {
    //        return Unauthorized(new { message = "User not authenticated" });
    //    }

    //    if (!ModelState.IsValid)
    //    {
    //        return BadRequest(ModelState);
    //    }

    //    location.UserId = userId;
    //    location.CreatedDate = DateTime.UtcNow;
    //    location.ModifiedDate = DateTime.UtcNow;

    //    var createdLocation = await _locationService.CreateLocationAsync(location);
    //    if (createdLocation == null)
    //    {
    //        return BadRequest(new { message = "Failed to create location. Please check validation rules." });
    //    }

    //    return CreatedAtAction(nameof(GetLocationById), new { id = createdLocation.Id }, createdLocation);
    //}

    ///// <summary>
    ///// Update an existing location
    ///// </summary>
    //[HttpPut("{id}")]
    //public async Task<ActionResult<Location>> UpdateLocation(int id, [FromBody] Location location)
    //{
    //    var userId = _authenticationService.GetCurrentUserInternalId();
    //    if (userId == 0)
    //    {
    //        return Unauthorized(new { message = "User not authenticated" });
    //    }

    //    if (!ModelState.IsValid)
    //    {
    //        return BadRequest(ModelState);
    //    }

    //    if (id != location.Id)
    //    {
    //        return BadRequest(new { message = "Location ID mismatch" });
    //    }

    //    var existingLocation = await _locationService.GetLocationByIdAsync(id, userId);
    //    if (existingLocation == null)
    //    {
    //        return NotFound(new { message = $"Location with ID {id} not found" });
    //    }

    //    location.UserId = userId;
    //    location.ModifiedDate = DateTime.UtcNow;

    //    try
    //    {
    //        var updatedLocation = await _locationService.UpdateLocationAsync(location);
    //        return Ok(updatedLocation);
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Error updating location {LocationId}", id);
    //        return BadRequest(new { message = $"Failed to update location: {ex.Message}" });
    //    }
    //}

    ///// <summary>
    ///// Delete a location
    ///// </summary>
    //[HttpDelete("{id}")]
    //public async Task<ActionResult> DeleteLocation(int id)
    //{
    //    var userId = _authenticationService.GetCurrentUserInternalId();
    //    if (userId == 0)
    //    {
    //        return Unauthorized(new { message = "User not authenticated" });
    //    }

    //    var existingLocation = await _locationService.GetLocationByIdAsync(id, userId);
    //    if (existingLocation == null)
    //    {
    //        return NotFound(new { message = $"Location with ID {id} not found" });
    //    }

    //    await _locationService.DeleteLocationAsync(id, userId);
    //    return NoContent();
    //}
}
