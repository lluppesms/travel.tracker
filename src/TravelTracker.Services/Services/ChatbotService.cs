using AutoGen.Core;
using Azure;
using Azure.AI.Agents.Persistent;
using Azure.AI.Inference;
using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos;
using Microsoft.VisualBasic;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
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
    //private readonly ChatCompletionsClient? _client;
    private readonly PersistentAgentsClient _chatClient;
    //private readonly DefaultAzureCredential _credential = new();
    private IConfiguration Configuration { get; }

    private readonly string systemPrompt =
        "You are a helpful travel assistant for the Travel Tracker application. " +
        "You help users find information about their travel locations, national parks, and location types. " +
        "Be conversational, helpful, and use the provided context data to answer questions accurately. " +
        "If the context data is empty or doesn't contain the information needed, politely let the user know.";


    public ChatbotService(
        ILocationService locationService,
        INationalParkService nationalParkService,
        ILocationTypeService locationTypeService,
        ILogger<ChatbotService> logger,
        IOptions<AzureAIFoundrySettings> settings,
        IConfiguration configuration)
    {
        _locationService = locationService;
        _nationalParkService = nationalParkService;
        _locationTypeService = locationTypeService;
        _logger = logger;
        _settings = settings.Value;
        Configuration = configuration;

        // Initialize client only if settings are configured
        if (!string.IsNullOrEmpty(_settings.Endpoint) && !string.IsNullOrEmpty(_settings.ApiKey))
        {
            var creds = CredentialsHelper.GetCredentials(Configuration);
            _chatClient = new(_settings.Endpoint, creds);
        }
    }

    public async Task<string> GetChatResponseAsync(string userMessage, int userId)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
        {
            return "Please provide a message.";
        }
        if (_chatClient == null)
        {
            return "The Chatbot is not configured properly. Please configure the Azure AI Foundry settings in the application settings!";
        }
        try
        {
            // Analyze the user's query and fetch relevant data
            var contextData = await GatherContextDataAsync(userMessage, userId);

            var enhancedSystemPrompt = $"{systemPrompt}\n\nContext data from the database:\n{contextData}";
            PersistentAgentThread thread = _chatClient.Threads.CreateThread();
            PersistentAgent chatAgent = _chatClient.Administration.CreateAgent(
                model: _settings.DeploymentName,
                name: "Travel Tracker Expert",
                instructions: enhancedSystemPrompt
            );
            var messageResponse = await _chatClient.Messages.CreateMessageAsync(thread.Id, MessageRole.User, userMessage);
            ThreadRun run = _chatClient.Runs.CreateRun(thread.Id, chatAgent.Id);
            do
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(500));
                run = _chatClient.Runs.GetRun(thread.Id, run.Id);
            }
            while (run.Status == RunStatus.Queued || run.Status == RunStatus.InProgress);
            if (run.Status != RunStatus.Completed) { throw new InvalidOperationException($"Run failed or was canceled: {run.LastError?.Message}"); }

            Pageable<PersistentThreadMessage> messages = _chatClient.Messages.GetMessages(thread.Id, order: ListSortOrder.Ascending);

            var messageContent = string.Empty;

            var agentMessages = from PersistentThreadMessage threadMessage in messages
                where threadMessage.Role == MessageRole.Agent
                from MessageContent contentItem in threadMessage.ContentItems
                where contentItem is MessageTextContent textItem
                select (MessageTextContent)contentItem;

            foreach (var msg in agentMessages)
            {
                messageContent += msg.Text;
            }

            return messageContent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chatbot request");
            var msg = $"I've encountered an error processing your request to {_settings.Endpoint}. Please try again later. {ex.Message}";
            return msg;
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
                    var summary = locations.Select(l => $"- {l.Name} in {l.City}, {l.State} ({l.LocationType}, Visited: {l.StartDate:yyyy-MM-dd})").Take(250);
                    contextParts.Add($"User's locations:\n{string.Join("\n", summary)}");
                    if (locations.Count() > 250)
                    {
                        contextParts.Add($"(showing 250 of {locations.Count()} total locations)");
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
                    var topStates = counts.OrderByDescending(kvp => kvp.Value).Take(10);
                    contextParts.Add($"Top states: {string.Join(", ", topStates.Select(kvp => $"{kvp.Key} ({kvp.Value})"))}");
                }
            }

            // Check if asking about national parks
            if (messageLower.Contains("national park") || messageLower.Contains("parks"))
            {
                var parks = await _nationalParkService.GetAllParksAsync();
                if (parks.Any())
                {
                    var summary = parks.Take(80).Select(p => $"- {p.Name} in {p.State}");
                    contextParts.Add($"National Parks in database:\n{string.Join("\n", summary)}");
                }
                var locations = await _locationService.GetAllLocationsAsync(userId);
                if (locations.Any())
                {
                    var parksVisited = locations.Where(l => l.LocationType == "National Park");
                    var summary = parksVisited.Select(l => $"- {l.Name} visited {l.StartDate:yyyy-MM-dd}").Take(250);
                    contextParts.Add($"National Parks Visited:\n{string.Join("\n", summary)}");
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
