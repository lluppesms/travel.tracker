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
    private const int TestUserId = 123;

    public ChatbotServiceTests()
    {
        _mockLocationService = new Mock<ILocationService>();
        _mockNationalParkService = new Mock<INationalParkService>();
        _mockLocationTypeService = new Mock<ILocationTypeService>();
        _mockLogger = new Mock<ILogger<ChatbotService>>();
        
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
            _emptySettings);

        // Act
        var result = await service.GetChatResponseAsync("Hello", TestUserId);

        // Assert
        Assert.Contains("not configured", result);
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
            _emptySettings);

        // Act
        var result = await service.GetChatResponseAsync("", TestUserId);

        // Assert
        Assert.Contains("provide a message", result);
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
            _emptySettings);

        // Act
        var result = await service.GetChatResponseAsync("   ", TestUserId);

        // Assert
        Assert.Contains("provide a message", result);
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
            _emptySettings);
        
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
            validSettings);
        
        Assert.NotNull(service);
    }
}
