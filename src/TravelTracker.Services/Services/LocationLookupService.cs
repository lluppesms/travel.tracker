using Azure;
using Azure.AI.Inference;
using System.Text.Json;

namespace TravelTracker.Services.Services;

public class LocationLookupService : ILocationLookupService
{
    private readonly AzureAIFoundrySettings _settings;
    private readonly ChatCompletionsClient? _client;
    private readonly ILogger<LocationLookupService> _logger;

    public bool IsConfigured => _client != null;

    private const int LookupMaxTokens = 300;

    private static readonly string SystemPrompt =
        """
        You are a precise location data validator.
        Given a location name and partial address details, validate or correct the address and find the exact latitude and longitude.
        Always respond with a valid JSON object only - no markdown formatting, no code blocks, just the raw JSON.
        """;

    public LocationLookupService(
        IOptions<AzureAIFoundrySettings> settings,
        ILogger<LocationLookupService> logger,
        IConfiguration configuration)
    {
        _settings = settings.Value;
        _logger = logger;

        if (!string.IsNullOrEmpty(_settings.Endpoint))
        {
            if (!string.IsNullOrEmpty(_settings.ApiKey))
            {
                _client = new ChatCompletionsClient(new Uri(_settings.Endpoint), new AzureKeyCredential(_settings.ApiKey));
            }
            else
            {
                _client = new ChatCompletionsClient(new Uri(_settings.Endpoint), CredentialsHelper.GetCredentials(configuration));
            }
        }
    }

    public async Task<LocationLookupResult> LookupLocationAsync(string name, string address, string city, string state, string zipCode)
    {
        if (_client == null)
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
            var requestOptions = new ChatCompletionsOptions
            {
                Messages =
                {
                    new ChatRequestSystemMessage(SystemPrompt),
                    new ChatRequestUserMessage(userPrompt)
                },
                Model = _settings.DeploymentName,
                MaxTokens = LookupMaxTokens
            };

            Console.WriteLine($"Endpoint: {_settings.Endpoint}");
            Console.WriteLine($"DeploymentName: {_settings.DeploymentName}");
            Console.WriteLine($"ApiKey: {_settings.ApiKey}");
            Console.WriteLine($"AgentId: {_settings.AgentId}");

            var response = await _client.CompleteAsync(requestOptions);
            var content = response.Value.Choices[0].Message.Content ?? string.Empty;

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
