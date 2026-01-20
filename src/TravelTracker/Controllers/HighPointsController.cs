using Microsoft.AspNetCore.Mvc;
using TravelTracker.Data.Models;
using RouteAttribute = Microsoft.AspNetCore.Mvc.RouteAttribute;

namespace TravelTracker.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/[controller]")]
public class HighPointsController : ControllerBase
{
    private readonly IHighPointService _highPointService;
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<HighPointsController> _logger;

    public HighPointsController(IHighPointService highPointService, IAuthenticationService authenticationService, ILogger<HighPointsController> logger)
    {
        _highPointService = highPointService;
        _authenticationService = authenticationService;
        _logger = logger;
    }

    /// <summary>
    /// Get all state high points
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<HighPoint>>> GetAllHighPoints()
    {
        var highPoints = await _highPointService.GetAllHighPointsAsync();
        return Ok(highPoints);
    }

    /// <summary>
    /// Get a specific state high point by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<HighPoint>> GetHighPointById(int id)
    {
        var highPoint = await _highPointService.GetHighPointByIdAsync(id);
        if (highPoint == null)
        {
            return NotFound(new { message = $"State high point with ID {id} not found" });
        }

        return Ok(highPoint);
    }

    /// <summary>
    /// Get state high points by state
    /// </summary>
    [HttpGet("by-state/{state}")]
    public async Task<ActionResult<IEnumerable<HighPoint>>> GetHighPointsByState(string state)
    {
        var highPoints = await _highPointService.GetHighPointsByStateAsync(state);
        return Ok(highPoints);
    }

    /// <summary>
    /// Get state high points visited by the authenticated user
    /// </summary>
    [HttpGet("visited/{userId}")]
    public async Task<ActionResult<IEnumerable<HighPoint>>> GetVisitedHighPoints(int userId)
    {
        var (validatedUserId, errorMessage) = _authenticationService.ValidateUserAccess(userId);
        if (validatedUserId == 0) { return Unauthorized(new { message = errorMessage }); }

        var visitedHighPoints = await _highPointService.GetVisitedHighPointsAsync(validatedUserId);
        return Ok(visitedHighPoints);
    }
}
