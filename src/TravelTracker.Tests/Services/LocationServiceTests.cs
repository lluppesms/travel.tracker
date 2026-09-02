using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using TravelTracker.Data.Models;
using TravelTracker.Data.Repositories;
using TravelTracker.Services.Models;
using TravelTracker.Services.Services;

namespace TravelTracker.Tests.Services;

public class LocationServiceTests
{
    [Fact]
    public async Task CreateLocationAsync_WhenRepositoryReturnsZeroId_ThrowsExplicitFailure()
    {
        var location = new Location
        {
            UserId = 7,
            Name = "Buffalo House RV Park",
            LocationType = "RV Park"
        };
        var locations = new Mock<ILocationRepository>();
        locations.Setup(value => value.CreateAsync(location, It.IsAny<CancellationToken>()))
            .ReturnsAsync(location);
        var types = new Mock<ILocationTypeRepository>();
        types.Setup(value => value.GetByNameAsync("RV Park"))
            .ReturnsAsync(new LocationType { Id = 3, Name = "RV Park" });
        var service = new LocationService(
            locations.Object,
            types.Object,
            NullLogger<LocationService>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateLocationAsync(location));
    }

    [Fact]
    public async Task SearchForAssistantAsync_ClampsResultsAndOmitsCommentsAndTags()
    {
        var locations = new Mock<ILocationRepository>();
        locations.Setup(value => value.SearchForAssistantAsync(
                7,
                "Buffalo",
                25,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new Location
                {
                    Id = 42,
                    UserId = 7,
                    Name = "Buffalo House RV Park",
                    LocationType = "RV Park",
                    City = "Duluth",
                    State = "MN",
                    StartDate = new DateTime(2026, 8, 31),
                    Comments = "private note",
                    Tags = ["private tag"]
                }
            ]);
        var service = new LocationService(
            locations.Object,
            Mock.Of<ILocationTypeRepository>(),
            NullLogger<LocationService>.Instance);

        var result = await service.SearchForAssistantAsync(7, "Buffalo", 100);

        Assert.Equal("untrusted_stored_text", result[0].TrustLabel);
        Assert.DoesNotContain(
            typeof(AssistantLocationSearchResult).GetProperties(),
            property => property.Name is nameof(Location.Comments) or nameof(Location.Tags));
    }
}
