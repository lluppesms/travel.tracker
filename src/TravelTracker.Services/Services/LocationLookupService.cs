using System.Text.Json;

namespace TravelTracker.Services.Services;

public class LocationLookupService : ILocationLookupService
{
    private readonly AzureAIFoundrySettings _settings;
    private readonly ILogger<LocationLookupService> _logger;
    private readonly IConfiguration _configuration;
    private ChatClient? _chatClient;

    public bool IsConfigured => !string.IsNullOrEmpty(_settings.Endpoint) && !string.IsNullOrEmpty(_settings.DeploymentName);

    private const string SystemPrompt =
        """
        You are a precise location finder.
        Given a location name and partial address details, get the exact latitude and longitude
        by calling this API: https://photon.komoot.io/api?q=<address>+<city>+<state>+<zipCode>&format=json
        Always respond with a valid JSON object only - no markdown formatting, no code blocks, just the raw JSON.
        """;
    // private const string SystemPrompt_V1 =
    //     """
    //     You are a precise location data validator.
    //     Given a location name and partial address details, validate or correct the address and find the exact latitude and longitude.
    //     If you can't find an EXACT match, return the closest match possible.  If a street address is provided, use that to find the latitude and longitude.
    //     Call this API if needed to get a lat/long: https://photon.komoot.io/api?q=<address>+<city>+<state>+<zipCode>&format=json
    //     Use Bing Maps, campground websites, national park websites, or campground directories as valid sources.
    //     Always respond with a valid JSON object only - no markdown formatting, no code blocks, just the raw JSON.
    //     """;
    public LocationLookupService(
        IOptions<AzureAIFoundrySettings> settings,
        ILogger<LocationLookupService> logger,
        IConfiguration configuration)
    {
        _settings = settings.Value;
        _logger = logger;
        _configuration = configuration;
    }

    private bool InitializeChatClient()
    {
        if (_chatClient != null) return true;

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

            _chatClient = azureClient.GetChatClient(_settings.DeploymentName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing Location Lookup ChatClient");
            return false;
        }
    }

    public async Task<LocationLookupResult> LookupLocationAsync(string name, string address, string city, string state, string zipCode)
    {
        if (!InitializeChatClient())
        {
            return new LocationLookupResult
            {
                Success = false,
                ErrorMessage = "AI lookup service is not configured. Please configure AzureAIFoundry settings."
            };
        }

        var userPrompt =
            $"Find and validate the location data for this place:\n" +
            $"Name: {name}\n" +
            $"Address: {(string.IsNullOrWhiteSpace(address) ? "(unknown)" : address)}\n" +
            $"City: {(string.IsNullOrWhiteSpace(city) ? "(unknown)" : city)}\n" +
            $"State: {(string.IsNullOrWhiteSpace(state) ? "(unknown)" : state)}\n" +
            $"ZipCode: {(string.IsNullOrWhiteSpace(zipCode) ? "(unknown)" : zipCode)}\n\n" +
            "Validate or correct the address, city, state, and zip code. Find the precise latitude and longitude.\n" +
            "Respond ONLY with a JSON object in this exact format (no extra text):\n" +
            "{\"success\":true,\"address\":\"street address\",\"city\":\"city\",\"state\":\"XX\",\"zipCode\":\"00000\",\"latitude\":0.000000,\"longitude\":0.000000}\n" +
            "If you cannot find the location, respond with:\n" +
            "{\"success\":false,\"errorMessage\":\"reason why it could not be found\"}";

        try
        {
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(SystemPrompt),
                new UserChatMessage(userPrompt)
            };

            var options = new ChatCompletionOptions();

            var response = await _chatClient!.CompleteChatAsync(messages, options);
            var content = response.Value.Content[0].Text ?? string.Empty;

            _logger.LogInformation("Location lookup AI response: {Content}", content);

            content = CleanJsonResponse(content);

            var result = JsonSerializer.Deserialize<LocationLookupResult>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? new LocationLookupResult { Success = false, ErrorMessage = "Failed to parse AI response." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error looking up location '{Name}' via AI", name);
            return new LocationLookupResult { Success = false, ErrorMessage = $"Lookup failed: {ex.Message}" };
        }
    }

    private static string CleanJsonResponse(string content)
    {
        content = content.Trim();

        // Strip markdown code blocks if present
        if (content.StartsWith("```"))
        {
            var lines = content.Split('\n');
            content = string.Join('\n', lines.Skip(1).TakeWhile(l => !l.StartsWith("```")));
        }

        return content.Trim();
    }
}
