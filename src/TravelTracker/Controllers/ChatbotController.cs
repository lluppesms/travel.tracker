using Microsoft.AspNetCore.Mvc;
using RouteAttribute = Microsoft.AspNetCore.Mvc.RouteAttribute;

namespace TravelTracker.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatbotController : ControllerBase
{
    private readonly IChatbotService _chatbotService;
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<ChatbotController> _logger;

    public ChatbotController(
        IChatbotService chatbotService,
        IAuthenticationService authenticationService,
        ILogger<ChatbotController> logger)
    {
        _chatbotService = chatbotService;
        _authenticationService = authenticationService;
        _logger = logger;
    }

    /// <summary>
    /// Send a message to the chatbot and get a response
    /// </summary>
    [HttpPost("message")]
    public async Task<ActionResult<ChatResponse>> SendMessage([FromBody] ChatRequest request)
    {
        var userId = _authenticationService.GetCurrentUserInternalId();
        if (userId == 0)
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { message = "Message cannot be empty" });
        }

        try
        {
            var (message, latestMessageDate, threadId) = await _chatbotService.GetChatResponseAsync(request.Message, userId, request.ThreadId, request.LastMessageDate);
            return Ok(new ChatResponse
            {
                Message = message,
                Timestamp = latestMessageDate?.UtcDateTime ?? DateTime.UtcNow,
                ThreadId = threadId,
                LatestMessageDate = latestMessageDate
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chatbot message");
            return StatusCode(500, new { message = "An error occurred processing your message" });
        }
    }
}

public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
    public string? ThreadId { get; set; }
    public DateTimeOffset? LastMessageDate { get; set; }
}

public class ChatResponse
{
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string ThreadId { get; set; } = string.Empty;
    public DateTimeOffset? LatestMessageDate { get; set; }
}
