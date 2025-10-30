using Azure.AI.Agents.Persistent;

namespace TravelTracker.Services.Services;

public class ChatbotService : IChatbotService
{
    private readonly ILocationService _locationService;
    private readonly INationalParkService _nationalParkService;
    private readonly ILocationTypeService _locationTypeService;
    private readonly ILogger<ChatbotService> _logger;
    private readonly AzureAIFoundrySettings _settings;
    private readonly PersistentAgentsClient? _chatClient; // make nullable to allow unconfigured state
    private IConfiguration Configuration { get; }

    private readonly string systemPrompt =
        "You are a helpful travel assistant for the Travel Tracker application. " +
        "You help users find information about their travel locations, national parks, and location types. " +
        "Be conversational, helpful, and use the provided context data to answer questions accurately. " +
        "If the context data is empty or doesn't contain the information needed, politely let the user know.";

    private string previousContextData = string.Empty;
    private string previousUserId = string.Empty;
    private string? _cachedAgentId = null; // Cache the agent ID to reuse across calls

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

    public async Task<(string message, DateTimeOffset? latestMessageDate, string threadId)> GetChatResponseAsync(string userMessage, int userId, string? threadId = null, DateTimeOffset? lastMessageDate = null)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
        {
            return ("Please provide a message.", lastMessageDate, threadId ?? string.Empty);
        }
        if (_chatClient == null)
        {
            return ("The Chatbot is not configured properly. Please configure the Azure AI Foundry settings in the application settings!", lastMessageDate, threadId ?? string.Empty);
        }

        PersistentAgentThread? thread = null;
        try
        {
            // If we have an existing thread id, reuse it; otherwise create a new thread
                if (!string.IsNullOrWhiteSpace(threadId))
            {
                try
                {
                    thread = _chatClient.Threads.GetThread(threadId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get existing thread {ThreadId}. Creating a new one.", threadId);
                }
            }
            thread ??= _chatClient.Threads.CreateThread();

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

            var enhancedSystemPrompt = $"{systemPrompt}\n\nContext data from the database:\n{contextData}";

            // Get or create the agent (reuses existing agent)
            var agentId = GetOrCreateAgent();

            // Update the agent's instructions with fresh context data
            _chatClient.Administration.UpdateAgent(
                assistantId: agentId,
                instructions: enhancedSystemPrompt
            );

            // Add user message to thread
            _ = await _chatClient.Messages.CreateMessageAsync(thread.Id, MessageRole.User, userMessage);

            // Start run and poll until completion (beta SDK currently requires polling)
            ThreadRun run = _chatClient.Runs.CreateRun(thread.Id, agentId);
            do
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(500));
                run = _chatClient.Runs.GetRun(thread.Id, run.Id);
            }
            while (run.Status == RunStatus.Queued || run.Status == RunStatus.InProgress);

            if (run.Status != RunStatus.Completed)
            {
                throw new InvalidOperationException($"Run failed or was canceled: {run.LastError?.Message}");
            }

            Pageable<PersistentThreadMessage> messages = _chatClient.Messages.GetMessages(thread.Id, order: ListSortOrder.Ascending);

            // Determine latest agent/user message timestamp
            DateTimeOffset? latestMessageDate = messages.Select(m => m.CreatedAt).DefaultIfEmpty(DateTimeOffset.UtcNow).Max();

            // Determine which message to start with
            var firstMessageDate = messages.Select(m => m.CreatedAt).DefaultIfEmpty(DateTimeOffset.UtcNow).Min().AddSeconds(-1);
            lastMessageDate = lastMessageDate  == null ? firstMessageDate : lastMessageDate;

            // Aggregate agent responses (some models may stream multiple chunks)
            var agentMessages = from PersistentThreadMessage threadMessage in messages
                                where threadMessage.Role == MessageRole.Agent && threadMessage.CreatedAt > lastMessageDate
                                from MessageContent contentItem in threadMessage.ContentItems
                                where contentItem is MessageTextContent
                                select new { contentItem = (MessageTextContent)contentItem, threadMessage.CreatedAt };

            var messageContent = string.Join(" ", agentMessages.Select(m => m.contentItem.Text)).Trim();
            if (string.IsNullOrEmpty(messageContent))
            {
                messageContent = "I didn't generate a response this time. Please try rephrasing your question.";
            }

            return (messageContent, latestMessageDate, thread.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chatbot request");
            var msg = $"I've encountered an error processing your request to {_settings.Endpoint}. Please try again later. {ex.Message}";
            return (msg, lastMessageDate, threadId ?? thread?.Id ?? string.Empty);
        }
    }

    private string GetOrCreateAgent()
    {
        if (_chatClient == null)
        {
            throw new InvalidOperationException("Chat client is not initialized");
        }

        // Return cached agent ID if available
        if (!string.IsNullOrEmpty(_cachedAgentId))
        {
            try
            {
                // Verify the agent still exists
                _chatClient.Administration.GetAgent(_cachedAgentId);
                _logger.LogInformation("Reusing existing agent {AgentId}", _cachedAgentId);
                return _cachedAgentId;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cached agent {AgentId} no longer exists, creating a new one", _cachedAgentId);
                _cachedAgentId = null;
            }
        }

        // Create a new agent with basic instructions (will be updated before each run)
        var agentResponse = _chatClient.Administration.CreateAgent(
            model: _settings.DeploymentName,
            name: "Travel Tracker Expert",
            instructions: systemPrompt
        );

        _cachedAgentId = agentResponse.Value.Id;
        _logger.LogInformation("Created new agent {AgentId}", _cachedAgentId);
        return _cachedAgentId;
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

            // Check if asking about national parks
            var parks = await _nationalParkService.GetAllParksAsync();
            if (parks.Any())
            {
                var summary = parks.Select(p => $"- {p.Name} in {p.State}");
                contextParts.Add($"National Parks in database:\n{string.Join("\n", summary)}");
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
    //private async Task<string> GatherDynamicContextDataAsync(string userMessage, int userId)
    //{
    //    var contextParts = new List<string>();
    //    var messageLower = userMessage.ToLower();

    //    try
    //    {
    //        // Check if asking about all locations
    //        if (Regex.IsMatch(messageLower, @"\b(all|my|show|list|view)\s+(locations?|places?|visits?)\b") ||
    //            messageLower.Contains("where have i") || messageLower.Contains("what places"))
    //        {
    //            var locations = await _locationService.GetAllLocationsAsync(userId);
    //            if (locations.Any())
    //            {
    //                var summary = locations.Select(l => $"- {l.Name} in {l.City}, {l.State} ({l.LocationType}, Visited: {l.StartDate:yyyy-MM-dd})").Take(250);
    //                contextParts.Add($"User's locations:\n{string.Join("\n", summary)}");
    //                if (locations.Count() > 250)
    //                {
    //                    contextParts.Add($"(showing 250 of {locations.Count()} total locations)");
    //                }
    //            }
    //        }

    //        // Check if asking about specific state
    //        var stateMatch = Regex.Match(messageLower, @"\b(in|from|at)\s+([a-z]{2,20})\b");
    //        if (stateMatch.Success)
    //        {
    //            var state = stateMatch.Groups[2].Value.ToUpper();
    //            var locations = await _locationService.GetLocationsByStateAsync(userId, state);
    //            if (locations.Any())
    //            {
    //                var summary = locations.Select(l => $"- {l.Name} in {l.City} ({l.LocationType}, Rating: {l.Rating}/5)");
    //                contextParts.Add($"Locations in {state}:\n{string.Join("\n", summary)}");
    //            }
    //        }

    //        // Check if asking about state counts/statistics
    //        if (Regex.IsMatch(messageLower, @"\b(how many|count|number of|total)\s+(states?|locations?)\b") ||
    //            messageLower.Contains("statistics") || messageLower.Contains("visited"))
    //        {
    //            var counts = await _locationService.GetLocationsByStateCountAsync(userId);
    //            if (counts.Any())
    //            {
    //                var totalStates = counts.Count;
    //                var totalLocations = counts.Values.Sum();
    //                contextParts.Add($"Travel statistics: {totalLocations} locations across {totalStates} states");
    //                var topStates = counts.OrderByDescending(kvp => kvp.Value).Take(10);
    //                contextParts.Add($"Top states: {string.Join(", ", topStates.Select(kvp => $"{kvp.Key} ({kvp.Value})"))}");
    //            }
    //        }

    //        // Check if asking about national parks
    //        if (messageLower.Contains("national park") || messageLower.Contains("parks"))
    //        {
    //            var parks = await _nationalParkService.GetAllParksAsync();
    //            if (parks.Any())
    //            {
    //                var summary = parks.Take(80).Select(p => $"- {p.Name} in {p.State}");
    //                contextParts.Add($"National Parks in database:\n{string.Join("\n", summary)}");
    //            }
    //            var locations = await _locationService.GetAllLocationsAsync(userId);
    //            if (locations.Any())
    //            {
    //                var parksVisited = locations.Where(l => l.LocationType == "National Park");
    //                var summary = parksVisited.Select(l => $"- {l.Name} visited {l.StartDate:yyyy-MM-dd}").Take(250);
    //                contextParts.Add($"National Parks Visited:\n{string.Join("\n", summary)}");
    //            }
    //        }

    //        // Check if asking about location types
    //        if (Regex.IsMatch(messageLower, @"\b(types?|kinds?|categories)\b") &&
    //            Regex.IsMatch(messageLower, @"\b(location|place)\b"))
    //        {
    //            var types = await _locationTypeService.GetAllLocationTypesAsync();
    //            if (types.Any())
    //            {
    //                var summary = types.Select(t => $"- {t.Name}: {t.Description}");
    //                contextParts.Add($"Location types:\n{string.Join("\n", summary)}");
    //            }
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogWarning(ex, "Error gathering context data for chatbot");
    //    }

    //    return contextParts.Any() ? string.Join("\n\n", contextParts) : string.Empty;
    //}
}
