using Microsoft.AspNetCore.Mvc;
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
    /// Get a specific national park by ID and state
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<NationalPark>> GetParkById(int id, [FromQuery] string state)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            return BadRequest(new { message = "State parameter is required" });
        }

        var park = await _nationalParkService.GetParkByIdAsync(id, state);
        if (park == null)
        {
            return NotFound(new { message = $"National park with ID {id} in state {state} not found" });
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
    [HttpGet("visited")]
    public async Task<ActionResult<IEnumerable<NationalPark>>> GetVisitedParks()
    {
        var userId = _authenticationService.GetCurrentUserInternalId();
        if (userId == 0)
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var visitedParks = await _nationalParkService.GetVisitedParksAsync(userId);
        return Ok(visitedParks);
    }
}
