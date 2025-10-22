using Moq;
using TravelTracker.Data.Models;
using TravelTracker.Data.Repositories;
using TravelTracker.Services.Services;

namespace TravelTracker.Tests.Services;

public class NationalParkServiceTests
{
    [Fact]
    public async Task GetAllParksAsync_ReturnsAllParks()
    {
        // Arrange
        var expectedParks = new List<NationalPark>
        {
            new NationalPark { Id = 1, Name = "Yellowstone", State = "WY" },
            new NationalPark { Id = 2, Name = "Yosemite", State = "CA" },
            new NationalPark { Id = 3, Name = "Grand Canyon", State = "AZ" }
        };

        var mockParkRepository = new Mock<INationalParkRepository>();
        var mockLocationRepository = new Mock<ILocationRepository>();
        
        mockParkRepository.Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(expectedParks);

        var service = new NationalParkService(mockParkRepository.Object, mockLocationRepository.Object);

        // Act
        var result = await service.GetAllParksAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
        mockParkRepository.Verify(repo => repo.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetParksByStateAsync_ReturnsParksInState()
    {
        // Arrange
        var state = "CA";
        var expectedParks = new List<NationalPark>
        {
            new NationalPark { Id = 1, Name = "Yosemite", State = "CA" },
            new NationalPark { Id = 2, Name = "Joshua Tree", State = "CA" }
        };

        var mockParkRepository = new Mock<INationalParkRepository>();
        var mockLocationRepository = new Mock<ILocationRepository>();
        
        mockParkRepository.Setup(repo => repo.GetByStateAsync(state))
            .ReturnsAsync(expectedParks);

        var service = new NationalParkService(mockParkRepository.Object, mockLocationRepository.Object);

        // Act
        var result = await service.GetParksByStateAsync(state);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, park => Assert.Equal(state, park.State));
        mockParkRepository.Verify(repo => repo.GetByStateAsync(state), Times.Once);
    }

    [Fact]
    public async Task GetVisitedParksAsync_ReturnsParksWithMatchingLocations()
    {
        // Arrange
        int userId = 123;
        
        var allParks = new List<NationalPark>
        {
            new NationalPark { Id = 1, Name = "Yellowstone", State = "WY" },
            new NationalPark { Id = 2, Name = "Yosemite", State = "CA" },
            new NationalPark { Id = 3, Name = "Grand Canyon", State = "AZ" }
        };

        var userLocations = new List<Location>
        {
            new Location { Id = 1, UserId = userId, Name = "Yellowstone National Park", State = "WY", LocationType = "National Park", Tags = new List<string> { "national-park" } },
            new Location { Id = 2, UserId = userId, Name = "Grand Canyon NP", State = "AZ", LocationType = "National Park", Tags = new List<string> { "national-park" } }
        };

        var mockParkRepository = new Mock<INationalParkRepository>();
        var mockLocationRepository = new Mock<ILocationRepository>();
        
        mockParkRepository.Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(allParks);
        mockLocationRepository.Setup(repo => repo.GetAllByUserIdAsync(userId))
            .ReturnsAsync(userLocations);

        var service = new NationalParkService(mockParkRepository.Object, mockLocationRepository.Object);

        // Act
        var result = await service.GetVisitedParksAsync(userId);

        // Assert
        Assert.NotNull(result);
        var visitedParks = result.ToList();
        Assert.Equal(2, visitedParks.Count);
        Assert.Contains(visitedParks, p => p.Name == "Yellowstone");
        Assert.Contains(visitedParks, p => p.Name == "Grand Canyon");
        mockParkRepository.Verify(repo => repo.GetAllAsync(), Times.Once);
        mockLocationRepository.Verify(repo => repo.GetAllByUserIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetVisitedParksAsync_ReturnsEmpty_WhenNoParksVisited()
    {
        // Arrange
        int userId = 123;
        
        var allParks = new List<NationalPark>
        {
            new NationalPark { Id = 1, Name = "Yellowstone", State = "WY" }
        };

        var userLocations = new List<Location>
        {
            new Location { Id = 1, UserId = userId, Name = "Some Hotel", State = "CA", Tags = new List<string>() }
        };

        var mockParkRepository = new Mock<INationalParkRepository>();
        var mockLocationRepository = new Mock<ILocationRepository>();
        
        mockParkRepository.Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(allParks);
        mockLocationRepository.Setup(repo => repo.GetAllByUserIdAsync(userId))
            .ReturnsAsync(userLocations);

        var service = new NationalParkService(mockParkRepository.Object, mockLocationRepository.Object);

        // Act
        var result = await service.GetVisitedParksAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
}
