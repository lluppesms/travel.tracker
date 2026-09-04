using Moq;
using TravelTracker.Data.Models;
using TravelTracker.Data.Repositories;
using TravelTracker.Services.Services;

namespace TravelTracker.Tests.Services;

public class DestinationServiceTests
{
    private readonly Mock<IDestinationRepository> _destinationRepository = new();
    private readonly Mock<IDestinationTypeRepository> _destinationTypeRepository = new();
    private readonly Mock<ILocationRepository> _locationRepository = new();
    private readonly DestinationService _service;

    public DestinationServiceTests()
    {
        _service = new DestinationService(
            _destinationRepository.Object,
            _destinationTypeRepository.Object,
            _locationRepository.Object);
    }

    [Fact]
    public async Task DestinationQueries_DelegateToRepositories()
    {
        var destination = new Destination { Id = 1, Name = "Park" };
        var type = new DestinationType { Id = 2, Name = "National Park" };
        _destinationRepository.Setup(repository => repository.GetAllAsync()).ReturnsAsync([destination]);
        _destinationRepository.Setup(repository => repository.GetByIdAsync(1)).ReturnsAsync(destination);
        _destinationRepository.Setup(repository => repository.GetByStateAsync("CA")).ReturnsAsync([destination]);
        _destinationRepository.Setup(repository => repository.GetByDestinationTypeIdAsync(2)).ReturnsAsync([destination]);
        _destinationRepository.Setup(repository => repository.GetByDestinationTypeNameAsync("National Park")).ReturnsAsync([destination]);
        _destinationTypeRepository.Setup(repository => repository.GetAllAsync()).ReturnsAsync([type]);

        Assert.Same(destination, (await _service.GetAllDestinationsAsync()).Single());
        Assert.Same(destination, await _service.GetDestinationByIdAsync(1));
        Assert.Same(destination, (await _service.GetDestinationsByStateAsync("CA")).Single());
        Assert.Same(destination, (await _service.GetDestinationsByTypeIdAsync(2)).Single());
        Assert.Same(destination, (await _service.GetDestinationsByTypeNameAsync("National Park")).Single());
        Assert.Same(type, (await _service.GetAllDestinationTypesAsync()).Single());
    }

    [Fact]
    public async Task GetVisitedDestinations_WithoutType_UsesAllAndMatchesSupportedLocationType()
    {
        var destination = new Destination { Id = 1, Name = "Yellowstone" };
        _destinationRepository.Setup(repository => repository.GetAllAsync()).ReturnsAsync([destination]);
        _locationRepository.Setup(repository => repository.GetAllByUserIdAsync(7)).ReturnsAsync([
            new Location { Name = "Trip to Yellowstone National Park", LocationType = "National Park" }]);

        var result = await _service.GetVisitedDestinationsAsync(7);

        Assert.Same(destination, result.Single());
        _destinationRepository.Verify(repository => repository.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetVisitedDestinations_WithType_UsesFilteredDestinations()
    {
        var destination = new Destination { Id = 1, Name = "Mount Rainier" };
        _destinationRepository.Setup(repository => repository.GetByDestinationTypeIdAsync(3)).ReturnsAsync([destination]);
        _locationRepository.Setup(repository => repository.GetAllByUserIdAsync(7)).ReturnsAsync([
            new Location { Name = "Mount Rainier", LocationType = "State High Point" }]);

        var result = await _service.GetVisitedDestinationsAsync(7, 3);

        Assert.Same(destination, result.Single());
        _destinationRepository.Verify(repository => repository.GetByDestinationTypeIdAsync(3), Times.Once);
        _destinationRepository.Verify(repository => repository.GetAllAsync(), Times.Never);
    }

    [Theory]
    [InlineData("Museum")]
    [InlineData("")]
    public async Task GetVisitedDestinations_ExcludesUnsupportedOrUnmatchedLocations(string locationType)
    {
        var destination = new Destination { Id = 1, Name = "Yellowstone" };
        _destinationRepository.Setup(repository => repository.GetAllAsync()).ReturnsAsync([destination]);
        _locationRepository.Setup(repository => repository.GetAllByUserIdAsync(7)).ReturnsAsync([
            new Location { Name = "Yellowstone", LocationType = locationType }]);

        var result = await _service.GetVisitedDestinationsAsync(7);

        Assert.Empty(result);
    }
}