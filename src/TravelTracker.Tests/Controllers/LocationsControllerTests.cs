using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TravelTracker.Controllers;
using TravelTracker.Data.Models;
using TravelTracker.Services.Interfaces;

namespace TravelTracker.Tests.Controllers;

public class LocationsControllerTests
{
    private readonly Mock<ILocationService> _mockLocationService;
    private readonly Mock<IAuthenticationService> _mockAuthService;
    private readonly Mock<ILogger<LocationsController>> _mockLogger;
    private readonly LocationsController _controller;
    private const int TestUserId = 123;

    public LocationsControllerTests()
    {
        _mockLocationService = new Mock<ILocationService>();
        _mockAuthService = new Mock<IAuthenticationService>();
        _mockLogger = new Mock<ILogger<LocationsController>>();

        _mockAuthService.Setup(x => x.ValidateUserAccess(TestUserId)).Returns((TestUserId, (string?)null));

        _controller = new LocationsController(
            _mockLocationService.Object,
            _mockAuthService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GetAllLocations_ReturnsOkWithLocations()
    {
        // Arrange
        var locations = new List<Location>
        {
            new Location { Id = 1, UserId = TestUserId, Name = "Location 1" },
            new Location { Id = 2, UserId = TestUserId, Name = "Location 2" }
        };
        _mockLocationService.Setup(s => s.GetAllLocationsAsync(TestUserId))
            .ReturnsAsync(locations);

        // Act
        var result = await _controller.GetAllLocations(TestUserId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedLocations = Assert.IsAssignableFrom<IEnumerable<Location>>(okResult.Value);
        Assert.Equal(2, returnedLocations.Count());
    }

    [Fact]
    public async Task GetAllLocations_WithUnauthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        _mockAuthService.Setup(x => x.ValidateUserAccess(TestUserId)).Returns((0, "User not authenticated"));

        // Act
        var result = await _controller.GetAllLocations(TestUserId);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetLocationById_WithValidId_ReturnsOkWithLocation()
    {
        // Arrange
        var location = new Location { Id = 1, UserId = TestUserId, Name = "Test Location" };
        _mockLocationService.Setup(s => s.GetLocationByIdAsync(1, TestUserId))
            .ReturnsAsync((Location?)location);

        // Act
        var result = await _controller.GetLocationById(1, TestUserId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedLocation = Assert.IsType<Location>(okResult.Value);
        Assert.Equal("Test Location", returnedLocation.Name);
    }

    [Fact]
    public async Task GetLocationById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        _mockLocationService.Setup(s => s.GetLocationByIdAsync(999, TestUserId))
            .ReturnsAsync((Location?)null);

        // Act
        var result = await _controller.GetLocationById(999, TestUserId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetLocationsByState_ReturnsOkWithLocations()
    {
        // Arrange
        var locations = new List<Location>
        {
            new Location { Id = 1, UserId = TestUserId, State = "CA" },
            new Location { Id = 2, UserId = TestUserId, State = "CA" }
        };
        _mockLocationService.Setup(s => s.GetLocationsByStateAsync(TestUserId, "CA"))
            .ReturnsAsync(locations);

        // Act
        var result = await _controller.GetLocationsByState("CA", TestUserId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedLocations = Assert.IsAssignableFrom<IEnumerable<Location>>(okResult.Value);
        Assert.Equal(2, returnedLocations.Count());
    }

    [Fact]
    public async Task GetLocationsByDateRange_WithValidDates_ReturnsOkWithLocations()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 12, 31);
        var locations = new List<Location>
        {
            new Location { Id = 1, UserId = TestUserId, StartDate = new DateTime(2024, 6, 15) }
        };
        _mockLocationService.Setup(s => s.GetLocationsByDateRangeAsync(TestUserId, startDate, endDate))
            .ReturnsAsync(locations);

        // Act
        var result = await _controller.GetLocationsByDateRange(TestUserId, startDate, endDate);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedLocations = Assert.IsAssignableFrom<IEnumerable<Location>>(okResult.Value);
        Assert.Single(returnedLocations);
    }

    [Fact]
    public async Task GetLocationsByDateRange_WithInvalidDates_ReturnsBadRequest()
    {
        // Arrange
        var startDate = new DateTime(2024, 12, 31);
        var endDate = new DateTime(2024, 1, 1);

        // Act
        var result = await _controller.GetLocationsByDateRange(TestUserId, startDate, endDate);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    //[Fact]
    //public async Task CreateLocation_WithValidData_ReturnsCreatedAtAction()
    //{
    //    // Arrange
    //    var location = new Location
    //    {
    //        Name = "New Location",
    //        LocationType = "RV Park",
    //        State = "CA",
    //        Rating = 5
    //    };
    //    var createdLocation = new Location
    //    {
    //        Id = 1,
    //        Name = "New Location",
    //        LocationType = "RV Park",
    //        State = "CA",
    //        Rating = 5,
    //        UserId = TestUserId
    //    };
    //    _mockLocationService.Setup(s => s.CreateLocationAsync(It.IsAny<Location>()))
    //        .ReturnsAsync((Location?)createdLocation);

    //    // Act
    //    var result = await _controller.CreateLocation(location);

    //    // Assert
    //    var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
    //    var returnedLocation = Assert.IsType<Location>(createdResult.Value);
    //    Assert.Equal("New Location", returnedLocation.Name);
    //    Assert.Equal(TestUserId, returnedLocation.UserId);
    //}

    //[Fact]
    //public async Task CreateLocation_WithInvalidData_ReturnsBadRequest()
    //{
    //    // Arrange
    //    var location = new Location
    //    {
    //        Name = "Invalid Location",
    //        LocationType = "InvalidType",
    //        State = "CA"
    //    };
    //    _mockLocationService.Setup(s => s.CreateLocationAsync(It.IsAny<Location>()))
    //        .ReturnsAsync((Location?)null);

    //    // Act
    //    var result = await _controller.CreateLocation(location);

    //    // Assert
    //    Assert.IsType<BadRequestObjectResult>(result.Result);
    //}

    //[Fact]
    //public async Task UpdateLocation_WithValidData_ReturnsOk()
    //{
    //    // Arrange
    //    var locationId = 1;
    //    var location = new Location
    //    {
    //        Id = locationId,
    //        Name = "Updated Location",
    //        LocationType = "RV Park",
    //        State = "CA",
    //        Rating = 4
    //    };
    //    _mockLocationService.Setup(s => s.GetLocationByIdAsync(locationId, TestUserId))
    //        .ReturnsAsync((Location?)location);
    //    _mockLocationService.Setup(s => s.UpdateLocationAsync(It.IsAny<Location>()))
    //        .ReturnsAsync((Location?)location);

    //    // Act
    //    var result = await _controller.UpdateLocation(locationId, location);

    //    // Assert
    //    var okResult = Assert.IsType<OkObjectResult>(result.Result);
    //    var returnedLocation = Assert.IsType<Location>(okResult.Value);
    //    Assert.Equal("Updated Location", returnedLocation.Name);
    //}

    //[Fact]
    //public async Task UpdateLocation_WithMismatchedId_ReturnsBadRequest()
    //{
    //    // Arrange
    //    var location = new Location { Id = 1, Name = "Test" };

    //    // Act
    //    var result = await _controller.UpdateLocation(2, location);

    //    // Assert
    //    Assert.IsType<BadRequestObjectResult>(result.Result);
    //}

    //[Fact]
    //public async Task UpdateLocation_WithNonExistentLocation_ReturnsNotFound()
    //{
    //    // Arrange
    //    var locationId = 999;
    //    var location = new Location { Id = locationId, Name = "Test" };
    //    _mockLocationService.Setup(s => s.GetLocationByIdAsync(locationId, TestUserId))
    //        .ReturnsAsync((Location?)null);

    //    // Act
    //    var result = await _controller.UpdateLocation(locationId, location);

    //    // Assert
    //    Assert.IsType<NotFoundObjectResult>(result.Result);
    //}

    //[Fact]
    //public async Task DeleteLocation_WithValidId_ReturnsNoContent()
    //{
    //    // Arrange
    //    var locationId = 1;
    //    var location = new Location { Id = locationId, UserId = TestUserId };
    //    _mockLocationService.Setup(s => s.GetLocationByIdAsync(locationId, TestUserId))
    //        .ReturnsAsync((Location?)location);

    //    // Act
    //    var result = await _controller.DeleteLocation(locationId);

    //    // Assert
    //    Assert.IsType<NoContentResult>(result);
    //    _mockLocationService.Verify(s => s.DeleteLocationAsync(locationId, TestUserId), Times.Once);
    //}

    //[Fact]
    //public async Task DeleteLocation_WithNonExistentLocation_ReturnsNotFound()
    //{
    //    // Arrange
    //    var locationId = 999;
    //    _mockLocationService.Setup(s => s.GetLocationByIdAsync(locationId, TestUserId))
    //        .ReturnsAsync((Location?)null);

    //    // Act
    //    var result = await _controller.DeleteLocation(locationId);

    //    // Assert
    //    Assert.IsType<NotFoundObjectResult>(result);
    //}

    [Fact]
    public async Task GetLocationsByStateCount_ReturnsOkWithCounts()
    {
        // Arrange
        var stateCounts = new Dictionary<string, int>
        {
            { "CA", 5 },
            { "NY", 3 }
        };
        _mockLocationService.Setup(s => s.GetLocationsByStateCountAsync(TestUserId))
            .ReturnsAsync(stateCounts);

        // Act
        var result = await _controller.GetLocationsByStateCount(TestUserId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedCounts = Assert.IsType<Dictionary<string, int>>(okResult.Value);
        Assert.Equal(2, returnedCounts.Count);
        Assert.Equal(5, returnedCounts["CA"]);
    }
}
