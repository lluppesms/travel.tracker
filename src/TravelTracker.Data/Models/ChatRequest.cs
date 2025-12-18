namespace TravelTracker.Data.Models;

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
