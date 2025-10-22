using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TravelTracker.Controllers;
using TravelTracker.Data.Models;
using TravelTracker.Services.Interfaces;

namespace TravelTracker.Tests.Controllers;

public class NationalParksControllerTests
{
    private readonly Mock<INationalParkService> _mockNationalParkService;
    private readonly Mock<IAuthenticationService> _mockAuthService;
    private readonly Mock<ILogger<NationalParksController>> _mockLogger;
    private readonly NationalParksController _controller;
    private const int TestUserId = 123;

    public NationalParksControllerTests()
    {
        _mockNationalParkService = new Mock<INationalParkService>();
        _mockAuthService = new Mock<IAuthenticationService>();
        _mockLogger = new Mock<ILogger<NationalParksController>>();
        
        _mockAuthService.Setup(x => x.GetCurrentUserInternalId()).Returns(TestUserId);
        
        _controller = new NationalParksController(
            _mockNationalParkService.Object,
            _mockAuthService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GetAllParks_ReturnsOkWithParks()
    {
        // Arrange
        var parks = new List<NationalPark>
        {
            new NationalPark { Id = 1, Name = "Yellowstone", State = "WY" },
            new NationalPark { Id = 2, Name = "Yosemite", State = "CA" }
        };
        _mockNationalParkService.Setup(s => s.GetAllParksAsync())
            .ReturnsAsync(parks);

        // Act
        var result = await _controller.GetAllParks();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedParks = Assert.IsAssignableFrom<IEnumerable<NationalPark>>(okResult.Value);
        Assert.Equal(2, returnedParks.Count());
    }

    [Fact]
    public async Task GetParkById_WithValidIdAndState_ReturnsOkWithPark()
    {
        // Arrange
        var park = new NationalPark { Id = 1, Name = "Yellowstone", State = "WY" };
        _mockNationalParkService.Setup(s => s.GetParkByIdAsync(1, "WY"))
            .ReturnsAsync(park);

        // Act
        var result = await _controller.GetParkById(1, "WY");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedPark = Assert.IsType<NationalPark>(okResult.Value);
        Assert.Equal("Yellowstone", returnedPark.Name);
    }

    [Fact]
    public async Task GetParkById_WithoutState_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetParkById(1, "");

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetParkById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        _mockNationalParkService.Setup(s => s.GetParkByIdAsync(999, "WY"))
            .ReturnsAsync((NationalPark?)null);

        // Act
        var result = await _controller.GetParkById(999, "WY");

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetParksByState_ReturnsOkWithParks()
    {
        // Arrange
        var parks = new List<NationalPark>
        {
            new NationalPark { Id = 1, Name = "Yosemite", State = "CA" },
            new NationalPark { Id = 2, Name = "Death Valley", State = "CA" }
        };
        _mockNationalParkService.Setup(s => s.GetParksByStateAsync("CA"))
            .ReturnsAsync(parks);

        // Act
        var result = await _controller.GetParksByState("CA");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedParks = Assert.IsAssignableFrom<IEnumerable<NationalPark>>(okResult.Value);
        Assert.Equal(2, returnedParks.Count());
    }

    [Fact]
    public async Task GetVisitedParks_ReturnsOkWithVisitedParks()
    {
        // Arrange
        var visitedParks = new List<NationalPark>
        {
            new NationalPark { Id = 1, Name = "Yellowstone", State = "WY" }
        };
        _mockNationalParkService.Setup(s => s.GetVisitedParksAsync(TestUserId))
            .ReturnsAsync(visitedParks);

        // Act
        var result = await _controller.GetVisitedParks();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedParks = Assert.IsAssignableFrom<IEnumerable<NationalPark>>(okResult.Value);
        Assert.Single(returnedParks);
    }

    [Fact]
    public async Task GetVisitedParks_WithUnauthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        _mockAuthService.Setup(x => x.GetCurrentUserInternalId()).Returns(0);

        // Act
        var result = await _controller.GetVisitedParks();

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }
}
