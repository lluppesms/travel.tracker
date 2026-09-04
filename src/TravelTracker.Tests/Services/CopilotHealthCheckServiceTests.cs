using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TravelTracker.Data.Configuration;
using TravelTracker.Services.Configuration;
using TravelTracker.Services.Interfaces;
using TravelTracker.Services.Services;

namespace TravelTracker.Tests.Services;

public class CopilotHealthCheckServiceTests
{
    [Fact]
    public async Task GetFailureReasons_WhenRuntimeNotReady_ReturnsConfigurationAndRuntimeFailures()
    {
        var runtime = new Mock<ICopilotRuntimeAccessor>();
        runtime.SetupGet(value => value.IsReady).Returns(false);
        var service = CreateService(runtime.Object, new TravelAssistantOptions());

        var failures = await service.GetFailureReasonsAsync();

        Assert.Contains("CopilotClient:Runtime", failures);
        Assert.Contains("TravelAssistant:FoundryEndpoint", failures);
        Assert.Contains("TravelAssistant:ModelDeploymentName", failures);
        runtime.Verify(value => value.PingAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task IsHealthy_WhenAllChecksPass_ReturnsTrue()
    {
        var runtime = new Mock<ICopilotRuntimeAccessor>();
        runtime.SetupGet(value => value.IsReady).Returns(true);
        runtime.Setup(value => value.PingAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var service = CreateService(runtime.Object, ConfiguredOptions());

        Assert.True(await service.IsHealthyAsync());
    }

    [Fact]
    public async Task GetFailureReasons_WhenPingFails_ReturnsPingFailure()
    {
        var runtime = new Mock<ICopilotRuntimeAccessor>();
        runtime.SetupGet(value => value.IsReady).Returns(true);
        runtime.Setup(value => value.PingAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var service = CreateService(runtime.Object, ConfiguredOptions());

        var failures = await service.GetFailureReasonsAsync();

        Assert.Equal(["CopilotClient:Ping"], failures);
    }

    [Fact]
    public async Task GetFailureReasons_WhenPingThrowsCancellation_ReturnsTimeoutFailure()
    {
        var runtime = new Mock<ICopilotRuntimeAccessor>();
        runtime.SetupGet(value => value.IsReady).Returns(true);
        runtime.Setup(value => value.PingAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());
        var service = CreateService(runtime.Object, ConfiguredOptions());

        var failures = await service.GetFailureReasonsAsync();

        Assert.Equal(["CopilotClient:PingTimeout"], failures);
    }

    [Fact]
    public async Task GetFailureReasons_WhenPingThrowsException_ReturnsPingFailure()
    {
        var runtime = new Mock<ICopilotRuntimeAccessor>();
        runtime.SetupGet(value => value.IsReady).Returns(true);
        runtime.Setup(value => value.PingAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("unavailable"));
        var service = CreateService(runtime.Object, ConfiguredOptions());

        var failures = await service.GetFailureReasonsAsync();

        Assert.Equal(["CopilotClient:Ping"], failures);
    }

    private static CopilotHealthCheckService CreateService(
        ICopilotRuntimeAccessor runtime,
        TravelAssistantOptions options)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [TravelAssistantOptionsValidator.AzureAdTenantIdKey] = "tenant",
            [TravelAssistantOptionsValidator.AzureAdClientIdKey] = "client",
            [TravelAssistantOptionsValidator.SqlConnectionStringKey] = "Server=localhost"
        }).Build();
        var optionsMonitor = new Mock<IOptionsMonitor<TravelAssistantOptions>>();
        optionsMonitor.SetupGet(value => value.CurrentValue).Returns(options);
        return new CopilotHealthCheckService(
            new Mock<ILogger<CopilotHealthCheckService>>().Object,
            runtime,
            configuration,
            optionsMonitor.Object);
    }

    private static TravelAssistantOptions ConfiguredOptions() => new()
    {
        FoundryEndpoint = "https://foundry.example",
        ModelDeploymentName = "model"
    };
}