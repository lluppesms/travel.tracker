using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TravelTracker.Controllers;
using TravelTracker.Data.Models;
using TravelTracker.Services.Interfaces;

namespace TravelTracker.Tests.Controllers;

public class LocationTypesControllerTests
{
    private readonly Mock<ILocationTypeService> _mockLocationTypeService;
    private readonly Mock<ILogger<LocationTypesController>> _mockLogger;
    private readonly LocationTypesController _controller;

    public LocationTypesControllerTests()
    {
        _mockLocationTypeService = new Mock<ILocationTypeService>();
        _mockLogger = new Mock<ILogger<LocationTypesController>>();
        
        _controller = new LocationTypesController(
            _mockLocationTypeService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GetAllLocationTypes_ReturnsOkWithLocationTypes()
    {
        // Arrange
        var locationTypes = new List<LocationType>
        {
            new LocationType { Id = 1, Name = "RV Park" },
            new LocationType { Id = 2, Name = "National Park" },
            new LocationType { Id = 3, Name = "State Park" }
        };
        _mockLocationTypeService.Setup(s => s.GetAllLocationTypesAsync())
            .ReturnsAsync(locationTypes);

        // Act
        var result = await _controller.GetAllLocationTypes();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedTypes = Assert.IsAssignableFrom<IEnumerable<LocationType>>(okResult.Value);
        Assert.Equal(3, returnedTypes.Count());
    }

    [Fact]
    public async Task GetLocationTypeById_WithValidId_ReturnsOkWithLocationType()
    {
        // Arrange
        var locationType = new LocationType { Id = 1, Name = "RV Park" };
        _mockLocationTypeService.Setup(s => s.GetLocationTypeByIdAsync(1))
            .ReturnsAsync(locationType);

        // Act
        var result = await _controller.GetLocationTypeById(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedType = Assert.IsType<LocationType>(okResult.Value);
        Assert.Equal("RV Park", returnedType.Name);
    }

    [Fact]
    public async Task GetLocationTypeById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        _mockLocationTypeService.Setup(s => s.GetLocationTypeByIdAsync(999))
            .ReturnsAsync((LocationType?)null);

        // Act
        var result = await _controller.GetLocationTypeById(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetLocationTypeByName_WithValidName_ReturnsOkWithLocationType()
    {
        // Arrange
        var locationType = new LocationType { Id = 1, Name = "RV Park" };
        _mockLocationTypeService.Setup(s => s.GetLocationTypeByNameAsync("RV Park"))
            .ReturnsAsync(locationType);

        // Act
        var result = await _controller.GetLocationTypeByName("RV Park");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedType = Assert.IsType<LocationType>(okResult.Value);
        Assert.Equal("RV Park", returnedType.Name);
    }

    [Fact]
    public async Task GetLocationTypeByName_WithInvalidName_ReturnsNotFound()
    {
        // Arrange
        _mockLocationTypeService.Setup(s => s.GetLocationTypeByNameAsync("Invalid Type"))
            .ReturnsAsync((LocationType?)null);

        // Act
        var result = await _controller.GetLocationTypeByName("Invalid Type");

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }
}
