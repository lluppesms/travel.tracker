using System.Security.Claims;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging;

using Moq;

using TravelTracker.Controllers;
using TravelTracker.Data.Models;
using TravelTracker.Data.Repositories;
using TravelTracker.Services;
using TravelTracker.Services.Interfaces;
using TravelTracker.Services.Models;

namespace TravelTracker.Tests.Controllers;

public class ChatbotControllerTests
{
    [Theory]
    [InlineData(nameof(ChatbotController.SendMessage), "message")]
    [InlineData(nameof(ChatbotController.ConfirmAction), "actions/{actionId}/confirm")]
    [InlineData(nameof(ChatbotController.CancelAction), "actions/{actionId}/cancel")]
    public void StateChangingEndpoints_RequireAntiforgery(string methodName, string route)
    {
        var method = typeof(ChatbotController).GetMethod(methodName);

        Assert.NotNull(method);
        Assert.NotNull(method.GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute), inherit: true).SingleOrDefault());
        Assert.Equal(
            route,
            Assert.IsAssignableFrom<HttpMethodAttribute>(
                method.GetCustomAttributes(typeof(HttpMethodAttribute), inherit: true).Single()).Template);
    }

    [Fact]
    public void PendingActionsEndpoint_UsesAuthorizedReadRoute()
    {
        var method = typeof(ChatbotController).GetMethod(nameof(ChatbotController.GetPendingActions));

        Assert.NotNull(method);
        Assert.Equal(
            "pending-actions",
            Assert.IsType<HttpGetAttribute>(
                method.GetCustomAttributes(typeof(HttpGetAttribute), inherit: true).Single()).Template);
    }

    private readonly Mock<IChatbotService> _mockChatbotService;
    private readonly Mock<ICurrentTravelUserResolver> _mockUserResolver;
    private readonly Mock<ITravelAssistantActionService> _mockActionService;
    private readonly Mock<ITravelAssistantActionConfirmationService> _mockConfirmationService;
    private readonly Mock<ILogger<ChatbotController>> _mockLogger;
    private readonly ChatbotController _controller;
    private const int TestUserId = 123;

    private static readonly TravelAssistantUserContext TestUserContext =
        new(TestUserId, "external-123", "Test User", "test@example.com");

    public ChatbotControllerTests()
    {
        _mockChatbotService = new Mock<IChatbotService>();
        _mockUserResolver = new Mock<ICurrentTravelUserResolver>();
        _mockActionService = new Mock<ITravelAssistantActionService>();
        _mockConfirmationService = new Mock<ITravelAssistantActionConfirmationService>();
        _mockLogger = new Mock<ILogger<ChatbotController>>();

        _mockUserResolver
            .Setup(x => x.ResolveAsync(It.IsAny<ClaimsPrincipal?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestUserContext);

        _controller = new ChatbotController(
            _mockChatbotService.Object,
            _mockUserResolver.Object,
            _mockActionService.Object,
            _mockConfirmationService.Object,
            new TravelAssistantReadiness(true, []),
            _mockLogger.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = CreateAuthenticatedPrincipal()
                }
            }
        };
    }

    [Fact]
    public async Task GetPendingActions_ReturnsOnlyTrustedUsersActions()
    {
        var action = new AssistantActionSummary
        {
            ActionId = Guid.NewGuid().ToString("N"),
            Summary = "Add Buffalo House RV Park for 2026-08-31",
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddHours(1),
            State = AssistantActionStates.Pending
        };
        _mockActionService
            .Setup(service => service.GetPendingActionsAsync(
                TestUserContext,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([action]);

        var result = await _controller.GetPendingActions();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(action, Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<AssistantActionSummary>>(ok.Value)));
    }

    [Fact]
    public async Task ConfirmAction_AcceptsOnlyOpaqueActionIdAndReturnsCreatedLocation()
    {
        var actionId = Guid.NewGuid().ToString("N");
        _mockConfirmationService
            .Setup(service => service.ConfirmActionAsync(
                TestUserContext,
                actionId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConfirmActionResult
            {
                Success = true,
                CreatedLocationId = 42,
                ActionState = AssistantActionStates.Confirmed,
                Summary = "Add Buffalo House RV Park for 2026-08-31"
            });

        var result = await _controller.ConfirmAction(actionId);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(42, Assert.IsType<ConfirmActionResult>(ok.Value).CreatedLocationId);
    }

    [Theory]
    [InlineData("action_not_found", 404)]
    [InlineData("action_forbidden", 403)]
    [InlineData("action_not_pending", 409)]
    [InlineData("action_expired", 410)]
    [InlineData("persistence_failed", 503)]
    public async Task ConfirmAction_WhenActionFails_MapsStableStatus(string errorCode, int expectedStatus)
    {
        var actionId = Guid.NewGuid().ToString("N");
        _mockConfirmationService
            .Setup(service => service.ConfirmActionAsync(
                TestUserContext,
                actionId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConfirmActionResult
            {
                Success = false,
                ErrorCode = errorCode,
                ErrorMessage = "Safe failure"
            });

        var result = await _controller.ConfirmAction(actionId);

        var response = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(expectedStatus, response.StatusCode);
    }

    [Fact]
    public async Task CancelAction_WhenPending_ReturnsCancelled()
    {
        var actionId = Guid.NewGuid().ToString("N");
        _mockConfirmationService
            .Setup(service => service.CancelActionAsync(
                TestUserContext,
                actionId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CancelActionResult
            {
                Success = true,
                ActionState = AssistantActionStates.Cancelled
            });

        var result = await _controller.CancelAction(actionId);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    private static ClaimsPrincipal CreateAuthenticatedPrincipal() =>
        new(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "external-123")],
            authenticationType: "TestAuth"));

    private static ChatResponse GetChatResponse(ObjectResult result) =>
        Assert.IsType<ChatResponse>(result.Value);

    [Fact]
    public async Task SendMessage_WithValidMessage_ReturnsOkWithResponse()
    {
        var request = new ChatRequest { Message = "What locations have I visited?" };
        const string expectedResponse = "You have visited 5 locations.";
        _mockChatbotService
            .Setup(s => s.GetChatResponseAsync(request.Message, TestUserId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ChatTurnResult.Success(expectedResponse, "thread-123", DateTimeOffset.UtcNow));

        var result = await _controller.SendMessage(request);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = GetChatResponse(okResult);
        Assert.Equal(expectedResponse, response.Message);
        Assert.True(response.Timestamp <= DateTime.UtcNow);
        Assert.Equal("thread-123", response.ThreadId);
        Assert.Null(response.ErrorCode);
        Assert.Equal(ChatThreadStatuses.Active, response.ThreadStatus);
    }

    [Fact]
    public async Task SendMessage_WithMatchingLegacyUserId_ReturnsOk()
    {
        var request = new ChatRequest { Message = "Hello" };
        _mockChatbotService
            .Setup(s => s.GetChatResponseAsync(request.Message, TestUserId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ChatTurnResult.Success("Hi", "thread-123", DateTimeOffset.UtcNow));

        var result = await _controller.SendMessage(request, TestUserId);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task SendMessage_WithMismatchedLegacyUserId_ReturnsForbidden()
    {
        var request = new ChatRequest { Message = "Hello" };

        var result = await _controller.SendMessage(request, TestUserId + 1);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, objectResult.StatusCode);
        Assert.Equal(ChatErrorCodes.Forbidden, GetChatResponse(objectResult).ErrorCode);
        _mockChatbotService.Verify(
            s => s.GetChatResponseAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SendMessage_WithEmptyMessage_ReturnsBadRequestWithInvalidRequestCode()
    {
        var request = new ChatRequest { Message = "" };

        var result = await _controller.SendMessage(request);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(400, objectResult.StatusCode);
        Assert.Equal(ChatErrorCodes.InvalidRequest, GetChatResponse(objectResult).ErrorCode);
    }

    [Fact]
    public async Task SendMessage_WithWhitespaceMessage_ReturnsBadRequest()
    {
        var request = new ChatRequest { Message = "   " };

        var result = await _controller.SendMessage(request);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(400, objectResult.StatusCode);
        Assert.Equal(ChatErrorCodes.InvalidRequest, GetChatResponse(objectResult).ErrorCode);
    }

    [Fact]
    public async Task SendMessage_WhenNotAuthenticated_ReturnsUnauthorizedWithStableErrorCode()
    {
        _mockUserResolver
            .Setup(x => x.ResolveAsync(It.IsAny<ClaimsPrincipal?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TravelAssistantUserContext?)null);
        var request = new ChatRequest { Message = "Test message" };

        var result = await _controller.SendMessage(request);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(401, objectResult.StatusCode);
        Assert.Equal(ChatErrorCodes.Unauthorized, GetChatResponse(objectResult).ErrorCode);
    }

    [Fact]
    public async Task SendMessage_WhenServiceThrowsException_ReturnsInternalServerErrorWithoutExceptionDetail()
    {
        var request = new ChatRequest { Message = "Test message" };
        _mockChatbotService
            .Setup(s => s.GetChatResponseAsync(request.Message, TestUserId, null, null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Secret endpoint https://internal.example.com failed"));

        var result = await _controller.SendMessage(request);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, objectResult.StatusCode);
        var response = GetChatResponse(objectResult);
        Assert.Equal(ChatErrorCodes.InternalError, response.ErrorCode);
        Assert.DoesNotContain("Secret endpoint", response.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("internal.example.com", response.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(ChatErrorCodes.ProviderUnavailable, 503)]
    [InlineData(ChatErrorCodes.RateLimited, 429)]
    [InlineData(ChatErrorCodes.ThreadNotFound, 404)]
    [InlineData(ChatErrorCodes.ActionConflict, 409)]
    [InlineData(ChatErrorCodes.ActionExpired, 410)]
    [InlineData(ChatErrorCodes.InvalidRequest, 400)]
    public async Task SendMessage_WhenProviderReturnsFailure_MapsToExpectedStatusCode(string errorCode, int expectedStatus)
    {
        var request = new ChatRequest { Message = "Test message" };
        _mockChatbotService
            .Setup(s => s.GetChatResponseAsync(request.Message, TestUserId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ChatTurnResult.Failure(errorCode, "Something went wrong.", "thread-123"));

        var result = await _controller.SendMessage(request);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(expectedStatus, objectResult.StatusCode);
        Assert.Equal(errorCode, GetChatResponse(objectResult).ErrorCode);
    }

    [Fact]
    public async Task SendMessage_WhenAssistantIsNotReady_ReturnsProviderUnavailableWithoutConfigurationKeys()
    {
        var controller = new ChatbotController(
            new DisabledChatbotService(),
            _mockUserResolver.Object,
            _mockActionService.Object,
            _mockConfirmationService.Object,
            new TravelAssistantReadiness(false, ["SqlServer:ConnectionString is required."]),
            _mockLogger.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = CreateAuthenticatedPrincipal() }
            }
        };

        var result = await controller.SendMessage(new ChatRequest { Message = "hi" });

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(503, objectResult.StatusCode);
        var response = GetChatResponse(objectResult);
        Assert.Equal(ChatErrorCodes.ProviderUnavailable, response.ErrorCode);
        Assert.DoesNotContain("SqlServer:ConnectionString", response.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendMessage_WhenAssistantIsNotReady_UsesRealResolver_StillReturnsProviderUnavailable()
    {
        // Composes the services the host actually registers when SQL is absent: readiness is false and
        // UnavailableTravelUserResolver always resolves to no user. Readiness must win over a 401.
        var controller = new ChatbotController(
            new DisabledChatbotService(),
            new UnavailableTravelUserResolver(),
            _mockActionService.Object,
            _mockConfirmationService.Object,
            new TravelAssistantReadiness(false, ["SqlServer:ConnectionString is required."]),
            _mockLogger.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = CreateAuthenticatedPrincipal() }
            }
        };

        var result = await controller.SendMessage(new ChatRequest { Message = "hi" });

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(503, objectResult.StatusCode);
        Assert.Equal(ChatErrorCodes.ProviderUnavailable, GetChatResponse(objectResult).ErrorCode);
    }
}
