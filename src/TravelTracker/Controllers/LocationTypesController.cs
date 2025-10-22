using Microsoft.AspNetCore.Mvc;
using TravelTracker.Data.Models;
using TravelTracker.Services.Interfaces;

namespace TravelTracker.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LocationTypesController : ControllerBase
{
    private readonly ILocationTypeService _locationTypeService;
    private readonly ILogger<LocationTypesController> _logger;

    public LocationTypesController(
        ILocationTypeService locationTypeService,
        ILogger<LocationTypesController> logger)
    {
        _locationTypeService = locationTypeService;
        _logger = logger;
    }

    /// <summary>
    /// Get all location types
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<LocationType>>> GetAllLocationTypes()
    {
        var locationTypes = await _locationTypeService.GetAllLocationTypesAsync();
        return Ok(locationTypes);
    }

    /// <summary>
    /// Get a specific location type by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<LocationType>> GetLocationTypeById(int id)
    {
        var locationType = await _locationTypeService.GetLocationTypeByIdAsync(id);
        if (locationType == null)
        {
            return NotFound(new { message = $"Location type with ID {id} not found" });
        }

        return Ok(locationType);
    }

    /// <summary>
    /// Get a specific location type by name
    /// </summary>
    [HttpGet("by-name/{name}")]
    public async Task<ActionResult<LocationType>> GetLocationTypeByName(string name)
    {
        var locationType = await _locationTypeService.GetLocationTypeByNameAsync(name);
        if (locationType == null)
        {
            return NotFound(new { message = $"Location type with name '{name}' not found" });
        }

        return Ok(locationType);
    }
}
