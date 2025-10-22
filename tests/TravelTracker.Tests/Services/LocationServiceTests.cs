using Moq;
using TravelTracker.Data.Models;
using TravelTracker.Data.Repositories;
using TravelTracker.Services.Services;

namespace TravelTracker.Tests.Services;

public class LocationServiceTests
{
    private Mock<ILocationTypeRepository> CreateMockLocationTypeRepository()
    {
        var mock = new Mock<ILocationTypeRepository>();
        var locationTypes = new List<LocationType>
        {
            new LocationType { Id = 1, Name = "National Park" },
            new LocationType { Id = 2, Name = "Hotel" },
            new LocationType { Id = 3, Name = "Restaurant" },
            new LocationType { Id = 7, Name = "Other" }
        };
        
        mock.Setup(repo => repo.GetAllAsync()).ReturnsAsync(locationTypes);
        mock.Setup(repo => repo.GetByNameAsync(It.IsAny<string>()))
            .ReturnsAsync((string name) => locationTypes.FirstOrDefault(lt => lt.Name == name));
        
        return mock;
    }

    private Mock<INationalParkRepository> CreateMockNationalParkRepository()
    {
        var mock = new Mock<INationalParkRepository>();
        var parks = new List<NationalPark>
        {
            new NationalPark { Id = 1, Name = "Yellowstone", State = "WY" },
            new NationalPark { Id = 2, Name = "Yosemite", State = "CA" }
        };
        
        mock.Setup(repo => repo.GetAllAsync()).ReturnsAsync(parks);
        
        return mock;
    }

    [Fact]
    public async Task GetAllLocationsAsync_ReturnsLocationsForUser()
    {
        int userId = 123;
        var expectedLocations = new List<Location>
        {
            new Location { Id = 1, UserId = userId, Name = "Test Location 1" },
            new Location { Id = 2, UserId = userId, Name = "Test Location 2" }
        };

        var mockRepository = new Mock<ILocationRepository>();
        mockRepository.Setup(repo => repo.GetAllByUserIdAsync(userId))
            .ReturnsAsync(expectedLocations);

        var mockLocationTypeRepo = CreateMockLocationTypeRepository();
        var mockNationalParkRepo = CreateMockNationalParkRepository();
        var service = new LocationService(mockRepository.Object, mockLocationTypeRepo.Object, mockNationalParkRepo.Object);

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
        var location = new Location 
        { 
            UserId = 123, 
            Name = "New Location",
            LocationType = "Hotel",
            State = "CA",
            Rating = 5
        };

        var mockRepository = new Mock<ILocationRepository>();
        mockRepository.Setup(repo => repo.CreateAsync(It.IsAny<Location>()))
            .ReturnsAsync(location);

        var mockLocationTypeRepo = CreateMockLocationTypeRepository();
        var mockNationalParkRepo = CreateMockNationalParkRepository();
        var service = new LocationService(mockRepository.Object, mockLocationTypeRepo.Object, mockNationalParkRepo.Object);

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
        int userId = 123;
        var locations = new List<Location>
        {
            new Location { Id = 1, UserId = userId, State = "CA" },
            new Location { Id = 2, UserId = userId, State = "CA" },
            new Location { Id = 3, UserId = userId, State = "NY" }
        };

        var mockRepository = new Mock<ILocationRepository>();
        mockRepository.Setup(repo => repo.GetAllByUserIdAsync(userId))
            .ReturnsAsync(locations);

        var mockLocationTypeRepo = CreateMockLocationTypeRepository();
        var mockNationalParkRepo = CreateMockNationalParkRepository();
        var service = new LocationService(mockRepository.Object, mockLocationTypeRepo.Object, mockNationalParkRepo.Object);

        // Act
        var result = await service.GetLocationsByStateCountAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal(2, result["CA"]);
        Assert.Equal(1, result["NY"]);
    }

    [Fact]
    public async Task UpdateLocationAsync_CallsRepositoryUpdate()
    {
        var location = new Location 
        { 
            Id = 1,
            UserId = 123, 
            Name = "Updated Location",
            LocationType = "Hotel",
            State = "NY",
            Rating = 4
        };

        var mockRepository = new Mock<ILocationRepository>();
        mockRepository.Setup(repo => repo.UpdateAsync(It.IsAny<Location>()))
            .ReturnsAsync(location);

        var mockLocationTypeRepo = CreateMockLocationTypeRepository();
        var mockNationalParkRepo = CreateMockNationalParkRepository();
        var service = new LocationService(mockRepository.Object, mockLocationTypeRepo.Object, mockNationalParkRepo.Object);

        // Act
        var result = await service.UpdateLocationAsync(location);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Location", result.Name);
        Assert.Equal(4, result.Rating);
        mockRepository.Verify(repo => repo.UpdateAsync(It.IsAny<Location>()), Times.Once);
    }

    [Fact]
    public async Task DeleteLocationAsync_CallsRepositoryDelete()
    {
        int locationId = 10;
        int userId = 123;

        var mockRepository = new Mock<ILocationRepository>();
        mockRepository.Setup(repo => repo.DeleteAsync(locationId, userId))
            .Returns(Task.CompletedTask);

        var mockLocationTypeRepo = CreateMockLocationTypeRepository();
        var mockNationalParkRepo = CreateMockNationalParkRepository();
        var service = new LocationService(mockRepository.Object, mockLocationTypeRepo.Object, mockNationalParkRepo.Object);

        // Act
        await service.DeleteLocationAsync(locationId, userId);

        // Assert
        mockRepository.Verify(repo => repo.DeleteAsync(locationId, userId), Times.Once);
    }

    [Fact]
    public async Task GetLocationsByDateRangeAsync_ReturnsFilteredLocations()
    {
        int userId = 123;
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 12, 31);
        var expectedLocations = new List<Location>
        {
            new Location { Id = 1, UserId = userId, Name = "Location 1", StartDate = new DateTime(2024, 6, 15) },
            new Location { Id = 2, UserId = userId, Name = "Location 2", StartDate = new DateTime(2024, 7, 20) }
        };

        var mockRepository = new Mock<ILocationRepository>();
        mockRepository.Setup(repo => repo.GetByDateRangeAsync(userId, startDate, endDate))
            .ReturnsAsync(expectedLocations);

        var mockLocationTypeRepo = CreateMockLocationTypeRepository();
        var mockNationalParkRepo = CreateMockNationalParkRepository();
        var service = new LocationService(mockRepository.Object, mockLocationTypeRepo.Object, mockNationalParkRepo.Object);

        // Act
        var result = await service.GetLocationsByDateRangeAsync(userId, startDate, endDate);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        mockRepository.Verify(repo => repo.GetByDateRangeAsync(userId, startDate, endDate), Times.Once);
    }

    [Fact]
    public async Task GetLocationsByStateAsync_ReturnsLocationsInState()
    {
        int userId = 123;
        var state = "CA";
        var expectedLocations = new List<Location>
        {
            new Location { Id = 1, UserId = userId, Name = "Location 1", State = "CA" },
            new Location { Id = 2, UserId = userId, Name = "Location 2", State = "CA" }
        };

        var mockRepository = new Mock<ILocationRepository>();
        mockRepository.Setup(repo => repo.GetByStateAsync(userId, state))
            .ReturnsAsync(expectedLocations);

        var mockLocationTypeRepo = CreateMockLocationTypeRepository();
        var mockNationalParkRepo = CreateMockNationalParkRepository();
        var service = new LocationService(mockRepository.Object, mockLocationTypeRepo.Object, mockNationalParkRepo.Object);

        // Act
        var result = await service.GetLocationsByStateAsync(userId, state);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, loc => Assert.Equal(state, loc.State));
        mockRepository.Verify(repo => repo.GetByStateAsync(userId, state), Times.Once);
    }
}
