namespace TravelTracker.Services.Interfaces;

public interface IChatbotService
{
    Task<(string message, DateTimeOffset? latestMessageDate, string threadId)> GetChatResponseAsync(string userMessage, int userId, string? threadId = null, DateTimeOffset? lastMessageDate = null);
}
