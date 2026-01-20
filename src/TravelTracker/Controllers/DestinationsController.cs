using Microsoft.AspNetCore.Mvc;
using TravelTracker.Data.Models;
using RouteAttribute = Microsoft.AspNetCore.Mvc.RouteAttribute;

namespace TravelTracker.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/[controller]")]
public class DestinationsController : ControllerBase
{
    private readonly IDestinationService _destinationService;
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<DestinationsController> _logger;

    public DestinationsController(
        IDestinationService destinationService, 
        IAuthenticationService authenticationService, 
        ILogger<DestinationsController> logger)
    {
        _destinationService = destinationService;
        _authenticationService = authenticationService;
        _logger = logger;
    }

    /// <summary>
    /// Get all destinations
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Destination>>> GetAllDestinations()
    {
        var destinations = await _destinationService.GetAllDestinationsAsync();
        return Ok(destinations);
    }

    /// <summary>
    /// Get all destination types
    /// </summary>
    [HttpGet("types")]
    public async Task<ActionResult<IEnumerable<DestinationType>>> GetAllDestinationTypes()
    {
        var types = await _destinationService.GetAllDestinationTypesAsync();
        return Ok(types);
    }

    /// <summary>
    /// Get a specific destination by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Destination>> GetDestinationById(int id)
    {
        var destination = await _destinationService.GetDestinationByIdAsync(id);
        if (destination == null)
        {
            return NotFound(new { message = $"Destination with ID {id} not found" });
        }

        return Ok(destination);
    }

    /// <summary>
    /// Get destinations by state
    /// </summary>
    [HttpGet("by-state/{state}")]
    public async Task<ActionResult<IEnumerable<Destination>>> GetDestinationsByState(string state)
    {
        var destinations = await _destinationService.GetDestinationsByStateAsync(state);
        return Ok(destinations);
    }

    /// <summary>
    /// Get destinations by type ID
    /// </summary>
    [HttpGet("by-type-id/{destinationTypeId}")]
    public async Task<ActionResult<IEnumerable<Destination>>> GetDestinationsByTypeId(int destinationTypeId)
    {
        var destinations = await _destinationService.GetDestinationsByTypeIdAsync(destinationTypeId);
        return Ok(destinations);
    }

    /// <summary>
    /// Get destinations by type name
    /// </summary>
    [HttpGet("by-type-name/{destinationTypeName}")]
    public async Task<ActionResult<IEnumerable<Destination>>> GetDestinationsByTypeName(string destinationTypeName)
    {
        var destinations = await _destinationService.GetDestinationsByTypeNameAsync(destinationTypeName);
        return Ok(destinations);
    }

    /// <summary>
    /// Get destinations visited by the authenticated user
    /// </summary>
    [HttpGet("visited/{userId}")]
    public async Task<ActionResult<IEnumerable<Destination>>> GetVisitedDestinations(
        int userId, 
        [FromQuery] int? destinationTypeId = null)
    {
        var (validatedUserId, errorMessage) = _authenticationService.ValidateUserAccess(userId);
        if (validatedUserId == 0) { return Unauthorized(new { message = errorMessage }); }

        var visitedDestinations = await _destinationService.GetVisitedDestinationsAsync(validatedUserId, destinationTypeId);
        return Ok(visitedDestinations);
    }
}
