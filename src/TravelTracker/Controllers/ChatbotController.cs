using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using TravelTracker.Services;
using TravelTracker.Services.Models;

using RouteAttribute = Microsoft.AspNetCore.Mvc.RouteAttribute;

namespace TravelTracker.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ChatbotController : ControllerBase
{
    private readonly IChatbotService _chatbotService;
    private readonly ICurrentTravelUserResolver _currentUserResolver;
    private readonly TravelAssistantReadiness _readiness;
    private readonly ILogger<ChatbotController> _logger;

    public ChatbotController(
        IChatbotService chatbotService,
        ICurrentTravelUserResolver currentUserResolver,
        TravelAssistantReadiness readiness,
        ILogger<ChatbotController> logger)
    {
        _chatbotService = chatbotService;
        _currentUserResolver = currentUserResolver;
        _readiness = readiness;
        _logger = logger;
    }

    /// <summary>
    /// Send a message to the chatbot and get a response.
    /// </summary>
    /// <param name="request">The chat request payload.</param>
    /// <param name="userId">
    /// Legacy user identifier. Ignored when it matches the authenticated user and rejected otherwise.
    /// Removed in the next API version; do not send it.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("message")]
    [ProducesResponseType(typeof(ChatResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ChatResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ChatResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ChatResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ChatResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ChatResponse>> SendMessage(
        [FromBody] ChatRequest request,
        [FromQuery] int? userId = null,
        CancellationToken cancellationToken = default)
    {
        // Readiness is a startup fact and is checked before identity, because a disabled assistant
        // cannot resolve a user and would otherwise report a misleading authentication failure.
        if (!_readiness.IsReady)
        {
            // Failures are configuration KEY names only and are logged, never returned to the caller.
            _logger.LogWarning(
                "Travel assistant is not configured; missing configuration: {MissingConfigurationKeys}",
                string.Join(", ", _readiness.Failures));
            return ErrorResult(ChatErrorCodes.ProviderUnavailable, "The travel assistant is not configured.", request?.ThreadId);
        }

        var userContext = await _currentUserResolver.ResolveAsync(User, cancellationToken);
        if (userContext is null)
        {
            return ErrorResult(ChatErrorCodes.Unauthorized, "Authentication is required to use the travel assistant.", request?.ThreadId);
        }

        if (LegacyUserIdPolicy.Evaluate(userContext, userId) == LegacyUserIdEvaluation.Mismatched)
        {
            _logger.LogWarning("Rejected chatbot request with a legacy userId query that does not match the authenticated user.");
            return ErrorResult(ChatErrorCodes.Forbidden, "You are not allowed to use the travel assistant on behalf of another user.", request?.ThreadId);
        }

        if (request is null || string.IsNullOrWhiteSpace(request.Message))
        {
            return ErrorResult(ChatErrorCodes.InvalidRequest, "Message cannot be empty", request?.ThreadId);
        }

        try
        {
            var result = await _chatbotService.GetChatResponseAsync(
                request.Message,
                userContext.UserId,
                request.ThreadId,
                request.LastMessageDate,
                cancellationToken);

            var response = ToChatResponse(result);
            return result.IsSuccess
                ? Ok(response)
                : StatusCode(result.HttpStatusCode, response);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chatbot message");
            return ErrorResult(ChatErrorCodes.InternalError, "An error occurred processing your message", request.ThreadId);
        }
    }

    private ObjectResult ErrorResult(string errorCode, string userSafeMessage, string? threadId)
    {
        var result = ChatTurnResult.Failure(errorCode, userSafeMessage, threadId ?? string.Empty);
        return StatusCode(result.HttpStatusCode, ToChatResponse(result));
    }

    private static ChatResponse ToChatResponse(ChatTurnResult result) => new()
    {
        Message = result.Message,
        Timestamp = result.LatestMessageDate?.UtcDateTime ?? DateTime.UtcNow,
        ThreadId = result.ThreadId,
        LatestMessageDate = result.LatestMessageDate,
        ErrorCode = result.ErrorCode,
        ThreadStatus = result.ThreadStatus,
        ToolStatuses = result.ToolStatuses.Count == 0
            ? null
            : result.ToolStatuses.Select(t => new ChatToolStatusDto
            {
                ToolName = t.ToolName,
                State = t.State.ToString(),
                Detail = t.Detail,
                DurationMs = t.DurationMs
            }).ToList(),
        PendingAction = result.PendingAction is null
            ? null
            : new ChatPendingActionDto
            {
                ActionId = result.PendingAction.ActionId,
                ActionType = result.PendingAction.ActionType,
                Title = result.PendingAction.Title,
                Summary = result.PendingAction.Summary,
                DisplayName = result.PendingAction.DisplayName,
                LocationText = result.PendingAction.LocationText,
                Date = result.PendingAction.Date,
                TypeName = result.PendingAction.TypeName,
                ExpiresAt = result.PendingAction.ExpiresAt
            }
    };
}
