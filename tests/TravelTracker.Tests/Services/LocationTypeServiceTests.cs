using Moq;
using TravelTracker.Data.Models;
using TravelTracker.Data.Repositories;
using TravelTracker.Services.Services;

namespace TravelTracker.Tests.Services;

public class LocationTypeServiceTests
{
    [Fact]
    public async Task GetAllLocationTypesAsync_ReturnsAllTypes()
    {
        var expectedTypes = new List<LocationType>
        {
            new LocationType { Id = 1, Name = "National Park" },
            new LocationType { Id = 2, Name = "Hotel" },
            new LocationType { Id = 3, Name = "Restaurant" }
        };

        var mockRepository = new Mock<ILocationTypeRepository>();
        mockRepository.Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(expectedTypes);

        var service = new LocationTypeService(mockRepository.Object);

        // Act
        var result = await service.GetAllLocationTypesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
        mockRepository.Verify(repo => repo.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetLocationTypeByIdAsync_ReturnsCorrectType()
    {
        int typeId = 1;
        var expectedType = new LocationType { Id = typeId, Name = "National Park" };

        var mockRepository = new Mock<ILocationTypeRepository>();
        mockRepository.Setup(repo => repo.GetByIdAsync(typeId))
            .ReturnsAsync(expectedType);

        var service = new LocationTypeService(mockRepository.Object);

        // Act
        var result = await service.GetLocationTypeByIdAsync(typeId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("National Park", result.Name);
        mockRepository.Verify(repo => repo.GetByIdAsync(typeId), Times.Once);
    }

    [Fact]
    public async Task GetLocationTypeByNameAsync_ReturnsCorrectType()
    {
        var typeName = "Hotel";
        var expectedType = new LocationType { Id = 2, Name = typeName };

        var mockRepository = new Mock<ILocationTypeRepository>();
        mockRepository.Setup(repo => repo.GetByNameAsync(typeName))
            .ReturnsAsync(expectedType);

        var service = new LocationTypeService(mockRepository.Object);

        // Act
        var result = await service.GetLocationTypeByNameAsync(typeName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(typeName, result.Name);
        mockRepository.Verify(repo => repo.GetByNameAsync(typeName), Times.Once);
    }

    [Fact]
    public async Task IsValidLocationTypeAsync_WithValidType_ReturnsTrue()
    {
        var typeName = "National Park";
        var expectedType = new LocationType { Id = 1, Name = typeName };

        var mockRepository = new Mock<ILocationTypeRepository>();
        mockRepository.Setup(repo => repo.GetByNameAsync(typeName))
            .ReturnsAsync(expectedType);

        var service = new LocationTypeService(mockRepository.Object);

        // Act
        var result = await service.IsValidLocationTypeAsync(typeName);

        // Assert
        Assert.True(result);
        mockRepository.Verify(repo => repo.GetByNameAsync(typeName), Times.Once);
    }

    [Fact]
    public async Task IsValidLocationTypeAsync_WithInvalidType_ReturnsFalse()
    {
        var typeName = "InvalidType";

        var mockRepository = new Mock<ILocationTypeRepository>();
        mockRepository.Setup(repo => repo.GetByNameAsync(typeName))
            .ReturnsAsync((LocationType?)null);

        var service = new LocationTypeService(mockRepository.Object);

        // Act
        var result = await service.IsValidLocationTypeAsync(typeName);

        // Assert
        Assert.False(result);
        mockRepository.Verify(repo => repo.GetByNameAsync(typeName), Times.Once);
    }

    [Fact]
    public async Task IsValidLocationTypeAsync_WithEmptyString_ReturnsFalse()
    {
        var mockRepository = new Mock<ILocationTypeRepository>();
        var service = new LocationTypeService(mockRepository.Object);

        // Act
        var result = await service.IsValidLocationTypeAsync("");

        // Assert
        Assert.False(result);
        mockRepository.Verify(repo => repo.GetByNameAsync(It.IsAny<string>()), Times.Never);
    }
}
