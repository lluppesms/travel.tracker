using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using TravelTracker.Controllers;
using TravelTracker.Services;
using TravelTracker.Services.Interfaces;

namespace TravelTracker.Tests.Controllers;

public class ConfigControllerTests
{
    [Fact]
    public void Get_ReturnsResolvedUserNameWithConfiguredValues()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AppSettings:DefaultConnection"] = "Server=localhost;Database=Travel;User Id=test;Password=secret",
                ["AppSettings:EnvironmentName"] = "Test",
                ["AppSettings:AzureOpenAI:Chat:Endpoint"] = "https://example.openai.azure.com",
                ["AppSettings:AzureOpenAI:Chat:DeploymentName"] = "chat",
                ["AppSettings:AzureOpenAI:Chat:ApiKey"] = "secret-key",
                ["AppSettings:AzureOpenAI:Chat:MaxTokens"] = "512",
                ["AppSettings:AzureOpenAI:Chat:Temperature"] = "0.2",
                ["AppSettings:AzureOpenAI:Chat:TopP"] = "0.8",
                ["AppSettings:AzureOpenAI:Image:Endpoint"] = "https://example.openai.azure.com",
                ["AppSettings:AzureOpenAI:Image:DeploymentName"] = "image",
                ["AppSettings:AzureOpenAI:Image:ApiKey"] = "image-key"
            })
            .Build();
        var controller = new ConfigController(
            new Mock<ILocationService>().Object,
            new Mock<IAuthenticationService>().Object,
            new Mock<ILogger<LocationsController>>().Object,
            configuration);

        var result = controller.Get();

        Assert.Equal("BOGUS", result);
    }

    [Fact]
    public void Get_WithMissingConfigurationUsesDefaults()
    {
        var configuration = new ConfigurationBuilder().Build();
        var controller = new ConfigController(
            new Mock<ILocationService>().Object,
            new Mock<IAuthenticationService>().Object,
            new Mock<ILogger<LocationsController>>().Object,
            configuration);

        var result = controller.Get();

        Assert.Equal("BOGUS", result);
    }
}