using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using TravelTracker.Controllers;
using TravelTracker.Services.Interfaces;

namespace TravelTracker.Tests.Controllers;

public class HealthCheckControllerTests
{
    private readonly Mock<ICopilotHealthCheckService> _healthCheckService = new();
    private readonly Mock<ILogger<HealthCheckController>> _logger = new();

    [Fact]
    public async Task Ready_WhenHealthy_ReturnsOk()
    {
        _healthCheckService.Setup(service => service.IsHealthyAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var controller = CreateController();

        var result = await controller.Ready(CancellationToken.None);

        Assert.IsType<OkResult>(result);
        _healthCheckService.Verify(service => service.GetFailureReasonsAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Ready_WhenUnhealthy_ReturnsServiceUnavailable()
    {
        _healthCheckService.Setup(service => service.IsHealthyAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _healthCheckService.Setup(service => service.GetFailureReasonsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(["SQL", "Foundry"]);
        var controller = CreateController();

        var result = await controller.Ready(CancellationToken.None);

        var status = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, status.StatusCode);
    }

    [Fact]
    public async Task Ready_WhenCheckThrows_ReturnsServiceUnavailable()
    {
        _healthCheckService.Setup(service => service.IsHealthyAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("failure"));
        var controller = CreateController();

        var result = await controller.Ready(CancellationToken.None);

        var status = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, status.StatusCode);
    }

    [Fact]
    public async Task Ready_WhenCancelled_RethrowsCancellation()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        _healthCheckService.Setup(service => service.IsHealthyAsync(source.Token))
            .ThrowsAsync(new OperationCanceledException(source.Token));
        var controller = CreateController();

        await Assert.ThrowsAsync<OperationCanceledException>(() => controller.Ready(source.Token));
    }

    [Fact]
    public void Live_ReturnsOk()
    {
        var result = CreateController().Live();

        Assert.IsType<OkResult>(result);
    }

    private HealthCheckController CreateController() =>
        new(_healthCheckService.Object, _logger.Object);
}