using TravelTracker.Services.Models;

namespace TravelTracker.Services.Services;

public class ChatbotService : IChatbotService
{
    private readonly ILocationService _locationService;
    private readonly IDestinationService _destinationService;
    private readonly ILocationTypeService _locationTypeService;
    private readonly ILogger<ChatbotService> _logger;
    private readonly AzureAIFoundrySettings _settings;
    private readonly IConfiguration _configuration;
    private AIAgent? _chatAgent;

    private readonly string systemPrompt =
        "You are a helpful travel assistant for the Travel Tracker application. " +
        "You help users find information about their travel locations, destinations (national parks, state high points, presidential libraries), and location types. " +
        "Be conversational, helpful, and use the provided context data to answer questions accurately. " +
        "If the context data is empty or doesn't contain the information needed, politely let the user know.";

    private string previousContextData = string.Empty;
    private string previousUserId = string.Empty;

    public ChatbotService(
        ILocationService locationService,
        IDestinationService destinationService,
        ILocationTypeService locationTypeService,
        ILogger<ChatbotService> logger,
        IOptions<AzureAIFoundrySettings> settings,
        IConfiguration configuration)
    {
        _locationService = locationService;
        _destinationService = destinationService;
        _locationTypeService = locationTypeService;
        _logger = logger;
        _settings = settings.Value;
        _configuration = configuration;
    }

    private bool InitializeAgent(string instructions)
    {
        if (string.IsNullOrEmpty(_settings.Endpoint) || string.IsNullOrEmpty(_settings.DeploymentName))
        {
            _logger.LogWarning("Azure OpenAI endpoint or deployment name not configured");
            return false;
        }

        try
        {
            AzureOpenAIClient azureClient;

            if (!string.IsNullOrEmpty(_settings.ApiKey))
            {
                azureClient = new AzureOpenAIClient(new Uri(_settings.Endpoint), new ApiKeyCredential(_settings.ApiKey));
            }
            else
            {
                azureClient = new AzureOpenAIClient(new Uri(_settings.Endpoint), CredentialsHelper.GetCredentials(_configuration));
            }

            var chatClient = azureClient.GetChatClient(_settings.DeploymentName);
            _chatAgent = chatClient.AsAIAgent(
                name: "TravelTrackerExpert",
                instructions: instructions
            );

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing Chatbot Agent");
            return false;
        }
    }

    public async Task<ChatTurnResult> GetChatResponseAsync(
        string userMessage,
        int userId,
        string? threadId = null,
        DateTimeOffset? lastMessageDate = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveThreadId = threadId ?? Guid.NewGuid().ToString();

        if (string.IsNullOrWhiteSpace(userMessage))
        {
            return ChatTurnResult.Failure(ChatErrorCodes.InvalidRequest, "Please provide a message.", effectiveThreadId);
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var contextData = string.Empty;
            // Gather domain context from our data sources
            if (previousContextData != string.Empty && previousUserId == userId.ToString())
            {
                _logger.LogInformation("Reusing previous context data for user {UserId}", userId);
                contextData = previousContextData;
            }
            else
            {
                _logger.LogInformation("Gathering new context data for user {UserId}", userId);
                contextData = await GatherContextDataAsync(userMessage, userId);
                previousContextData = contextData;
                previousUserId = userId.ToString();
            }

            var enhancedInstructions = $"{systemPrompt}\n\nContext data from the database:\n{contextData}";

            // Re-initialize agent with fresh context each time
            if (!InitializeAgent(enhancedInstructions))
            {
                _logger.LogError("Chatbot agent could not be initialized for user {UserId}", userId);
                return ChatTurnResult.Failure(
                    ChatErrorCodes.ProviderUnavailable,
                    "The travel assistant is not available right now. Please try again later.",
                    effectiveThreadId);
            }

            var response = await _chatAgent!.RunAsync(userMessage, cancellationToken: cancellationToken);
            var messageContent = response.ToString()?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(messageContent))
            {
                messageContent = "I didn't generate a response this time. Please try rephrasing your question.";
            }

            return ChatTurnResult.Success(messageContent, effectiveThreadId, DateTimeOffset.UtcNow);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chatbot request for user {UserId}", userId);
            return ChatTurnResult.Failure(
                ChatErrorCodes.ProviderUnavailable,
                "I encountered a problem processing your request. Please try again later.",
                effectiveThreadId);
        }
    }

    private async Task<string> GatherContextDataAsync(string userMessage, int userId)
    {
        var contextParts = new List<string>();
        var messageLower = userMessage.ToLower();

        try
        {
            // Check if asking about all locations
            var locations = await _locationService.GetAllLocationsAsync(userId);
            if (locations.Any())
            {
                var summary = locations.Select(l => $"- {l.Name} in {l.City}, {l.State} ({l.LocationType}, Visited: {l.StartDate:yyyy-MM-dd})")
                    //.OrderByDescending(l => l.StartDate)
                    .Take(250);
                contextParts.Add($"User's locations:\n{string.Join("\n", summary)}");
                if (locations.Count() > 250)
                {
                    contextParts.Add($"(only showing the first 250 of {locations.Count()} total locations)");
                }
            }

            // Check if asking about state counts/statistics
            var counts = await _locationService.GetLocationsByStateCountAsync(userId);
            if (counts.Any())
            {
                var totalStates = counts.Count;
                var totalLocations = counts.Values.Sum();
                contextParts.Add($"Travel statistics: {totalLocations} locations across {totalStates} states");
                var topStates = counts.OrderByDescending(kvp => kvp.Value).Take(10);
                contextParts.Add($"Top states: {string.Join(", ", topStates.Select(kvp => $"{kvp.Key} ({kvp.Value})"))}");
            }

            // Check if asking about destinations (national parks, state high points, presidential libraries)
            var destinations = await _destinationService.GetAllDestinationsAsync();
            if (destinations.Any())
            {
                var summary = destinations.Select(d => $"- {d.Name} in {d.State}");
                contextParts.Add($"Destinations in database:\n{string.Join("\n", summary)}");
            }

            if (locations.Any())
            {
                var parksVisited = locations.Where(l => l.LocationType == "National Park");
                var summary = parksVisited.Select(l => $"- {l.Name} visited {l.StartDate:yyyy-MM-dd}");
                contextParts.Add($"National Parks Visited:\n{string.Join("\n", summary)}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error gathering context data for chatbot");
        }

        return contextParts.Any() ? string.Join("\n\n", contextParts) : string.Empty;
    }
}
