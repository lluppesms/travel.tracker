using Moq;
using TravelTracker.Data.Models;
using TravelTracker.Data.Repositories;
using TravelTracker.Services.Services;

namespace TravelTracker.Tests.Services;

public class LocationServiceTests
{
    [Fact]
    public async Task GetAllLocationsAsync_ReturnsLocationsForUser()
    {
        // Arrange
        var userId = "user123";
        var expectedLocations = new List<Location>
        {
            new Location { Id = "1", UserId = userId, Name = "Test Location 1" },
            new Location { Id = "2", UserId = userId, Name = "Test Location 2" }
        };

        var mockRepository = new Mock<ILocationRepository>();
        mockRepository.Setup(repo => repo.GetAllByUserIdAsync(userId))
            .ReturnsAsync(expectedLocations);

        var service = new LocationService(mockRepository.Object);

        // Act
        var result = await service.GetAllLocationsAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        mockRepository.Verify(repo => repo.GetAllByUserIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task CreateLocationAsync_CallsRepositoryCreate()
    {
        // Arrange
        var location = new Location 
        { 
            UserId = "user123", 
            Name = "New Location",
            State = "CA",
            Rating = 5
        };

        var mockRepository = new Mock<ILocationRepository>();
        mockRepository.Setup(repo => repo.CreateAsync(It.IsAny<Location>()))
            .ReturnsAsync(location);

        var service = new LocationService(mockRepository.Object);

        // Act
        var result = await service.CreateLocationAsync(location);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Location", result.Name);
        mockRepository.Verify(repo => repo.CreateAsync(It.IsAny<Location>()), Times.Once);
    }

    [Fact]
    public async Task GetLocationsByStateCountAsync_ReturnsCorrectCounts()
    {
        // Arrange
        var userId = "user123";
        var locations = new List<Location>
        {
            new Location { Id = "1", UserId = userId, State = "CA" },
            new Location { Id = "2", UserId = userId, State = "CA" },
            new Location { Id = "3", UserId = userId, State = "NY" }
        };

        var mockRepository = new Mock<ILocationRepository>();
        mockRepository.Setup(repo => repo.GetAllByUserIdAsync(userId))
            .ReturnsAsync(locations);

        var service = new LocationService(mockRepository.Object);

        // Act
        var result = await service.GetLocationsByStateCountAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal(2, result["CA"]);
        Assert.Equal(1, result["NY"]);
    }
}
