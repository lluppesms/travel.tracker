using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TravelTracker.Data.Configuration;
using TravelTracker.Data.Models;
using TravelTracker.Services.Interfaces;
using TravelTracker.Services.Services;

namespace TravelTracker.Tests.Services;

public class ChatbotServiceTests
{
    private readonly Mock<ILocationService> _mockLocationService;
    private readonly Mock<INationalParkService> _mockNationalParkService;
    private readonly Mock<ILocationTypeService> _mockLocationTypeService;
    private readonly Mock<ILogger<ChatbotService>> _mockLogger;
    private readonly IOptions<AzureAIFoundrySettings> _emptySettings;
    private readonly IConfiguration _configuration; // added
    private const int TestUserId = 123;

    public ChatbotServiceTests()
    {
        _mockLocationService = new Mock<ILocationService>();
        _mockNationalParkService = new Mock<INationalParkService>();
        _mockLocationTypeService = new Mock<ILocationTypeService>();
        _mockLogger = new Mock<ILogger<ChatbotService>>();
        _configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>()).Build();
        
        // Use empty settings for most tests (not configured)
        _emptySettings = Options.Create(new AzureAIFoundrySettings
        {
            Endpoint = "",
            ApiKey = "",
            DeploymentName = ""
        });
    }

    [Fact]
    public async Task GetChatResponseAsync_WhenNotConfigured_ReturnsConfigurationMessage()
    {
        // Arrange
        var service = new ChatbotService(
            _mockLocationService.Object,
            _mockNationalParkService.Object,
            _mockLocationTypeService.Object,
            _mockLogger.Object,
            _emptySettings,
            _configuration);

        // Act
        var (message, latest, threadId) = await service.GetChatResponseAsync("Hello", TestUserId);

        // Assert
        Assert.Contains("not configured", message.ToLower());
        Assert.True(string.IsNullOrEmpty(threadId));
    }

    [Fact]
    public async Task GetChatResponseAsync_WithEmptyMessage_ReturnsErrorMessage()
    {
        // Arrange
        var service = new ChatbotService(
            _mockLocationService.Object,
            _mockNationalParkService.Object,
            _mockLocationTypeService.Object,
            _mockLogger.Object,
            _emptySettings,
            _configuration);

        // Act
        var (message, latest, threadId) = await service.GetChatResponseAsync("", TestUserId);

        // Assert
        Assert.Contains("provide a message", message.ToLower());
    }

    [Fact]
    public async Task GetChatResponseAsync_WithWhitespaceMessage_ReturnsErrorMessage()
    {
        // Arrange
        var service = new ChatbotService(
            _mockLocationService.Object,
            _mockNationalParkService.Object,
            _mockLocationTypeService.Object,
            _mockLogger.Object,
            _emptySettings,
            _configuration);

        // Act
        var (message, latest, threadId) = await service.GetChatResponseAsync("   ", TestUserId);

        // Assert
        Assert.Contains("provide a message", message.ToLower());
    }

    [Fact]
    public void ChatbotService_Construction_WithEmptySettings_Succeeds()
    {
        // Act & Assert - should not throw
        var service = new ChatbotService(
            _mockLocationService.Object,
            _mockNationalParkService.Object,
            _mockLocationTypeService.Object,
            _mockLogger.Object,
            _emptySettings,
            _configuration);
        
        Assert.NotNull(service);
    }

    [Fact]
    public void ChatbotService_Construction_WithValidSettings_Succeeds()
    {
        // Arrange
        var validSettings = Options.Create(new AzureAIFoundrySettings
        {
            Endpoint = "https://test.openai.azure.com/",
            ApiKey = "test-key",
            DeploymentName = "test-deployment"
        });

        // Act & Assert - should not throw
        var service = new ChatbotService(
            _mockLocationService.Object,
            _mockNationalParkService.Object,
            _mockLocationTypeService.Object,
            _mockLogger.Object,
            validSettings,
            _configuration);
        
        Assert.NotNull(service);
    }

    [Fact]
    public void ChatbotService_Construction_WithAgentId_LogsInitialization()
    {
        // Arrange
        var testAgentId = "test-agent-id-12345";
        var settingsWithAgentId = Options.Create(new AzureAIFoundrySettings
        {
            Endpoint = "https://test.openai.azure.com/",
            ApiKey = "test-key",
            DeploymentName = "test-deployment",
            AgentId = testAgentId
        });

        // Act
        var service = new ChatbotService(
            _mockLocationService.Object,
            _mockNationalParkService.Object,
            _mockLocationTypeService.Object,
            _mockLogger.Object,
            settingsWithAgentId,
            _configuration);

        // Assert
        Assert.NotNull(service);
        // Verify that logger was called with information about initializing from configuration
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Initialized agent cache") && v.ToString()!.Contains(testAgentId)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void ChatbotService_Construction_WithEmptyAgentId_DoesNotInitializeCache()
    {
        // Arrange
        var settingsWithEmptyAgentId = Options.Create(new AzureAIFoundrySettings
        {
            Endpoint = "https://test.openai.azure.com/",
            ApiKey = "test-key",
            DeploymentName = "test-deployment",
            AgentId = ""
        });

        // Act
        var service = new ChatbotService(
            _mockLocationService.Object,
            _mockNationalParkService.Object,
            _mockLocationTypeService.Object,
            _mockLogger.Object,
            settingsWithEmptyAgentId,
            _configuration);

        // Assert
        Assert.NotNull(service);
        // Verify that logger was NOT called about initializing from configuration
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Initialized agent cache")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }
}
