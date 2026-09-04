using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TravelTracker.Controllers;
using TravelTracker.Data.Models;
using TravelTracker.Services.Interfaces;

namespace TravelTracker.Tests.Controllers;

public class DestinationsControllerTests
{
    private const int TestUserId = 123;
    private readonly Mock<IDestinationService> _destinationService = new();
    private readonly Mock<IAuthenticationService> _authenticationService = new();
    private readonly DestinationsController _controller;

    public DestinationsControllerTests()
    {
        _controller = new DestinationsController(
            _destinationService.Object,
            _authenticationService.Object,
            new Mock<ILogger<DestinationsController>>().Object);
    }

    [Fact]
    public async Task GetAllDestinations_ReturnsOk()
    {
        var destinations = new[] { new Destination { Id = 1, Name = "Beach" } };
        _destinationService.Setup(service => service.GetAllDestinationsAsync()).ReturnsAsync(destinations);

        var result = await _controller.GetAllDestinations();

        Assert.Single(Assert.IsAssignableFrom<IEnumerable<Destination>>(Assert.IsType<OkObjectResult>(result.Result).Value));
    }

    [Fact]
    public async Task GetAllDestinationTypes_ReturnsOk()
    {
        var types = new[] { new DestinationType { Id = 1, Name = "National Park" } };
        _destinationService.Setup(service => service.GetAllDestinationTypesAsync()).ReturnsAsync(types);

        var result = await _controller.GetAllDestinationTypes();

        Assert.Single(Assert.IsAssignableFrom<IEnumerable<DestinationType>>(Assert.IsType<OkObjectResult>(result.Result).Value));
    }

    [Fact]
    public async Task GetDestinationById_WhenFound_ReturnsOk()
    {
        var destination = new Destination { Id = 7, Name = "Beach" };
        _destinationService.Setup(service => service.GetDestinationByIdAsync(7)).ReturnsAsync(destination);

        var result = await _controller.GetDestinationById(7);

        Assert.Same(destination, Assert.IsType<OkObjectResult>(result.Result).Value);
    }

    [Fact]
    public async Task GetDestinationById_WhenMissing_ReturnsNotFound()
    {
        _destinationService.Setup(service => service.GetDestinationByIdAsync(7)).ReturnsAsync((Destination?)null);

        var result = await _controller.GetDestinationById(7);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Theory]
    [InlineData("CA")]
    [InlineData("RV Park")]
    public async Task DestinationFilters_ReturnOk(string value)
    {
        var destinations = new[] { new Destination { Id = 1, Name = value } };
        _destinationService.Setup(service => service.GetDestinationsByStateAsync(value)).ReturnsAsync(destinations);
        _destinationService.Setup(service => service.GetDestinationsByTypeNameAsync(value)).ReturnsAsync(destinations);

        var byState = await _controller.GetDestinationsByState(value);
        var byName = await _controller.GetDestinationsByTypeName(value);

        Assert.IsType<OkObjectResult>(byState.Result);
        Assert.IsType<OkObjectResult>(byName.Result);
    }

    [Fact]
    public async Task GetDestinationsByTypeId_ReturnsOk()
    {
        _destinationService.Setup(service => service.GetDestinationsByTypeIdAsync(4))
            .ReturnsAsync(Array.Empty<Destination>());

        var result = await _controller.GetDestinationsByTypeId(4);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetVisitedDestinations_WithValidUser_ReturnsOk()
    {
        _authenticationService.Setup(service => service.ValidateUserAccess(TestUserId))
            .Returns((TestUserId, (string?)null));
        _destinationService.Setup(service => service.GetVisitedDestinationsAsync(TestUserId, 4))
            .ReturnsAsync(Array.Empty<Destination>());

        var result = await _controller.GetVisitedDestinations(TestUserId, 4);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetVisitedDestinations_WithInvalidUser_ReturnsUnauthorized()
    {
        _authenticationService.Setup(service => service.ValidateUserAccess(TestUserId))
            .Returns((0, "User not authenticated"));

        var result = await _controller.GetVisitedDestinations(TestUserId);

        Assert.IsType<UnauthorizedObjectResult>(result.Result);
        _destinationService.Verify(service => service.GetVisitedDestinationsAsync(It.IsAny<int>(), It.IsAny<int?>()), Times.Never);
    }
}