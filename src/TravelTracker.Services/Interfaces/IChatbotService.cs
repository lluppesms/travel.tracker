namespace TravelTracker.Services.Interfaces;

public interface IChatbotService
{
    Task<string> GetChatResponseAsync(string userMessage, int userId);
}
