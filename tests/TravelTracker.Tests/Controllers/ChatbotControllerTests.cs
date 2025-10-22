using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TravelTracker.Controllers;
using TravelTracker.Services.Interfaces;

namespace TravelTracker.Tests.Controllers;

public class ChatbotControllerTests
{
    private readonly Mock<IChatbotService> _mockChatbotService;
    private readonly Mock<IAuthenticationService> _mockAuthService;
    private readonly Mock<ILogger<ChatbotController>> _mockLogger;
    private readonly ChatbotController _controller;
    private const int TestUserId = 123;

    public ChatbotControllerTests()
    {
        _mockChatbotService = new Mock<IChatbotService>();
        _mockAuthService = new Mock<IAuthenticationService>();
        _mockLogger = new Mock<ILogger<ChatbotController>>();

        _mockAuthService.Setup(x => x.GetCurrentUserInternalId()).Returns(TestUserId);

        _controller = new ChatbotController(
            _mockChatbotService.Object,
            _mockAuthService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task SendMessage_WithValidMessage_ReturnsOkWithResponse()
    {
        // Arrange
        var request = new ChatRequest { Message = "What locations have I visited?" };
        var expectedResponse = "You have visited 5 locations.";
        _mockChatbotService.Setup(s => s.GetChatResponseAsync(request.Message, TestUserId))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.SendMessage(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ChatResponse>(okResult.Value);
        Assert.Equal(expectedResponse, response.Message);
        Assert.True(response.Timestamp <= DateTime.UtcNow);
    }

    [Fact]
    public async Task SendMessage_WithEmptyMessage_ReturnsBadRequest()
    {
        // Arrange
        var request = new ChatRequest { Message = "" };

        // Act
        var result = await _controller.SendMessage(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task SendMessage_WithWhitespaceMessage_ReturnsBadRequest()
    {
        // Arrange
        var request = new ChatRequest { Message = "   " };

        // Act
        var result = await _controller.SendMessage(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task SendMessage_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        // Arrange
        _mockAuthService.Setup(x => x.GetCurrentUserInternalId()).Returns(0);
        var request = new ChatRequest { Message = "Test message" };

        // Act
        var result = await _controller.SendMessage(request);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        Assert.NotNull(unauthorizedResult.Value);
    }

    [Fact]
    public async Task SendMessage_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var request = new ChatRequest { Message = "Test message" };
        _mockChatbotService.Setup(s => s.GetChatResponseAsync(request.Message, TestUserId))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _controller.SendMessage(request);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }
}
