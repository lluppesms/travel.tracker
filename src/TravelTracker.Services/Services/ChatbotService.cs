using System.Text.Json;
using System.Text.RegularExpressions;
using Azure;
using Azure.AI.Inference;
using Azure.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TravelTracker.Data.Configuration;
using TravelTracker.Services.Interfaces;

namespace TravelTracker.Services.Services;

public class ChatbotService : IChatbotService
{
    private readonly ILocationService _locationService;
    private readonly INationalParkService _nationalParkService;
    private readonly ILocationTypeService _locationTypeService;
    private readonly ILogger<ChatbotService> _logger;
    private readonly AzureAIFoundrySettings _settings;
    private readonly ChatCompletionsClient? _client;

    public ChatbotService(
        ILocationService locationService,
        INationalParkService nationalParkService,
        ILocationTypeService locationTypeService,
        ILogger<ChatbotService> logger,
        IOptions<AzureAIFoundrySettings> settings)
    {
        _locationService = locationService;
        _nationalParkService = nationalParkService;
        _locationTypeService = locationTypeService;
        _logger = logger;
        _settings = settings.Value;

        // Initialize client only if settings are configured
        if (!string.IsNullOrEmpty(_settings.Endpoint) && !string.IsNullOrEmpty(_settings.ApiKey))
        {
            _client = new ChatCompletionsClient(
                new Uri(_settings.Endpoint),
                new AzureKeyCredential(_settings.ApiKey));
        }
    }

    public async Task<string> GetChatResponseAsync(string userMessage, int userId)
    {
        if (_client == null)
        {
            return "Chatbot is not configured. Please configure Azure AI Foundry settings in appsettings.json.";
        }

        if (string.IsNullOrWhiteSpace(userMessage))
        {
            return "Please provide a message.";
        }

        try
        {
            // Analyze the user's query and fetch relevant data
            var contextData = await GatherContextDataAsync(userMessage, userId);
            
            var systemPrompt = "You are a helpful travel assistant for the Travel Tracker application. " +
                "You help users find information about their travel locations, national parks, and location types. " +
                "Be conversational, helpful, and use the provided context data to answer questions accurately. " +
                "If the context data is empty or doesn't contain the information needed, politely let the user know.";

            if (!string.IsNullOrEmpty(contextData))
            {
                systemPrompt += $"\n\nContext data from the database:\n{contextData}";
            }

            var messages = new List<ChatRequestMessage>
            {
                new ChatRequestSystemMessage(systemPrompt),
                new ChatRequestUserMessage(userMessage)
            };

            var chatCompletionsOptions = new ChatCompletionsOptions(messages)
            {
                Model = _settings.DeploymentName
            };

            var response = await _client.CompleteAsync(chatCompletionsOptions);
            return response.Value.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chatbot request");
            return $"I encountered an error processing your request. Please try again later.";
        }
    }

    private async Task<string> GatherContextDataAsync(string userMessage, int userId)
    {
        var contextParts = new List<string>();
        var messageLower = userMessage.ToLower();

        try
        {
            // Check if asking about all locations
            if (Regex.IsMatch(messageLower, @"\b(all|my|show|list|view)\s+(locations?|places?|visits?)\b") ||
                messageLower.Contains("where have i") || messageLower.Contains("what places"))
            {
                var locations = await _locationService.GetAllLocationsAsync(userId);
                if (locations.Any())
                {
                    var summary = locations.Select(l => $"- {l.Name} in {l.City}, {l.State} ({l.LocationType}, Rating: {l.Rating}/5, Visited: {l.StartDate:yyyy-MM-dd})").Take(20);
                    contextParts.Add($"User's locations:\n{string.Join("\n", summary)}");
                    if (locations.Count() > 20)
                    {
                        contextParts.Add($"(showing 20 of {locations.Count()} total locations)");
                    }
                }
            }

            // Check if asking about specific state
            var stateMatch = Regex.Match(messageLower, @"\b(in|from|at)\s+([a-z]{2,20})\b");
            if (stateMatch.Success)
            {
                var state = stateMatch.Groups[2].Value.ToUpper();
                var locations = await _locationService.GetLocationsByStateAsync(userId, state);
                if (locations.Any())
                {
                    var summary = locations.Select(l => $"- {l.Name} in {l.City} ({l.LocationType}, Rating: {l.Rating}/5)");
                    contextParts.Add($"Locations in {state}:\n{string.Join("\n", summary)}");
                }
            }

            // Check if asking about state counts/statistics
            if (Regex.IsMatch(messageLower, @"\b(how many|count|number of|total)\s+(states?|locations?)\b") ||
                messageLower.Contains("statistics") || messageLower.Contains("visited"))
            {
                var counts = await _locationService.GetLocationsByStateCountAsync(userId);
                if (counts.Any())
                {
                    var totalStates = counts.Count;
                    var totalLocations = counts.Values.Sum();
                    contextParts.Add($"Travel statistics: {totalLocations} locations across {totalStates} states");
                    var topStates = counts.OrderByDescending(kvp => kvp.Value).Take(5);
                    contextParts.Add($"Top states: {string.Join(", ", topStates.Select(kvp => $"{kvp.Key} ({kvp.Value})"))}");
                }
            }

            // Check if asking about national parks
            if (messageLower.Contains("national park") || messageLower.Contains("parks"))
            {
                var parks = await _nationalParkService.GetAllParksAsync();
                if (parks.Any())
                {
                    var summary = parks.Take(10).Select(p => $"- {p.Name} in {p.State}");
                    contextParts.Add($"National Parks in database:\n{string.Join("\n", summary)}");
                    if (parks.Count() > 10)
                    {
                        contextParts.Add($"(showing 10 of {parks.Count()} total parks)");
                    }
                }
            }

            // Check if asking about location types
            if (Regex.IsMatch(messageLower, @"\b(types?|kinds?|categories)\b") && 
                Regex.IsMatch(messageLower, @"\b(location|place)\b"))
            {
                var types = await _locationTypeService.GetAllLocationTypesAsync();
                if (types.Any())
                {
                    var summary = types.Select(t => $"- {t.Name}: {t.Description}");
                    contextParts.Add($"Location types:\n{string.Join("\n", summary)}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error gathering context data for chatbot");
        }

        return contextParts.Any() ? string.Join("\n\n", contextParts) : string.Empty;
    }
}
