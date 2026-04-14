namespace TravelTracker.Mcp;

[AllowAnonymous]
[McpServerToolType]
public class ChatbotTools
{
    private readonly IChatbotService _chatbotService;
    private readonly IAuthenticationService _authenticationService;

    public ChatbotTools(IChatbotService chatbotService, IAuthenticationService authenticationService)
    {
        _chatbotService = chatbotService;
        _authenticationService = authenticationService;
    }

    [McpServerTool(Name = "send_message_to_chatbot")]
    [Description("Send a message to the AI travel assistant and get a response. The chatbot can answer questions about your travel history, locations visited, and provide travel recommendations. Requires authentication.")]
    public async Task<ChatbotResponse> SendMessageToChatbot(
        [Description("The message or question to send to the chatbot")] string message,
        [Description("Optional thread ID to continue an existing conversation")] string? threadId = null,
        [Description("Optional timestamp of the last message in the thread")] DateTimeOffset? lastMessageDate = null)
    {
        var userId = _authenticationService.GetCurrentUserInternalId();
        if (userId == 0)
        {
            throw new UnauthorizedAccessException("User not authenticated");
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Message cannot be empty");
        }

        var (responseMessage, latestMessageDate, responseThreadId) =
            await _chatbotService.GetChatResponseAsync(message, userId, threadId, lastMessageDate);

        return new ChatbotResponse
        {
            Message = responseMessage,
            ThreadId = responseThreadId,
            Timestamp = latestMessageDate?.UtcDateTime ?? DateTime.UtcNow,
            LatestMessageDate = latestMessageDate
        };
    }
}

public class ChatbotResponse
{
    public string Message { get; set; } = string.Empty;

    public string ThreadId { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; }

    public DateTimeOffset? LatestMessageDate { get; set; }
}
