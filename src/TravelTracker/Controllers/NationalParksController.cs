using Microsoft.AspNetCore.Mvc;
using TravelTracker.Data.Models;
using RouteAttribute = Microsoft.AspNetCore.Mvc.RouteAttribute;

namespace TravelTracker.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/[controller]")]
public class NationalParksController : ControllerBase
{
    private readonly INationalParkService _nationalParkService;
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<NationalParksController> _logger;

    public NationalParksController(INationalParkService nationalParkService, IAuthenticationService authenticationService, ILogger<NationalParksController> logger)
    {
        _nationalParkService = nationalParkService;
        _authenticationService = authenticationService;
        _logger = logger;
    }

    /// <summary>
    /// Get all national parks
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<NationalPark>>> GetAllParks()
    {
        var parks = await _nationalParkService.GetAllParksAsync();
        return Ok(parks);
    }

    /// <summary>
    /// Get a specific national park by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<NationalPark>> GetParkById(int id)
    {
        var park = await _nationalParkService.GetParkByIdAsync(id);
        if (park == null)
        {
            return NotFound(new { message = $"National park with ID {id} not found" });
        }

        return Ok(park);
    }

    /// <summary>
    /// Get national parks by state
    /// </summary>
    [HttpGet("by-state/{state}")]
    public async Task<ActionResult<IEnumerable<NationalPark>>> GetParksByState(string state)
    {
        var parks = await _nationalParkService.GetParksByStateAsync(state);
        return Ok(parks);
    }

    /// <summary>
    /// Get national parks visited by the authenticated user
    /// </summary>
    [HttpGet("visited/{userId}")]
    public async Task<ActionResult<IEnumerable<NationalPark>>> GetVisitedParks(int userId)
    {
        var (validatedUserId, errorMessage) = _authenticationService.ValidateUserAccess(userId);
        if (validatedUserId == 0) { return Unauthorized(new { message = errorMessage }); }

        var visitedParks = await _nationalParkService.GetVisitedParksAsync(validatedUserId);
        return Ok(visitedParks);
    }
}
