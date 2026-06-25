using System.Text.Json;
using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
//using Azure.AI.Projects.OpenAI;
using OpenAI.Responses;

namespace TravelTracker.Services.Services;

public class LocationLookupService(
    IOptions<AzureAIFoundrySettings> settings,
    IConfiguration configuration,
    LocationLookupAPIService apiFallback,
    ILogger<LocationLookupService> logger) : ILocationLookupService
{
    private readonly AzureAIFoundrySettings _settings = settings.Value;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<LocationLookupService> _logger = logger;
    private readonly LocationLookupAPIService _apiFallback = apiFallback;

    public bool IsConfigured => !string.IsNullOrEmpty(_settings.ProjectEndpoint)
                             && !string.IsNullOrEmpty(_settings.AgentName);

    public async Task<LocationLookupResult> LookupLocationAsync(string name, string address, string city, string state, string zipCode)
    {
        _logger.LogInformation(
            "LocationLookupService called with Name='{Name}', Address='{Address}', City='{City}', State='{State}', ZipCode='{ZipCode}'",
            name, address, city, state, zipCode);

        if (!IsConfigured)
        {
            _logger.LogInformation("AI Foundry is not configured, using public API fallback.");
            return await _apiFallback.LookupLocationAsync(name, address, city, state, zipCode);
        }

        try
        {
            var projectClient = new AIProjectClient(
                endpoint: new Uri(_settings.ProjectEndpoint),
                tokenProvider: CredentialsHelper.GetCredentials(_configuration));

            var agent = new AgentReference(_settings.AgentName, _settings.AgentVersion);
            _logger.LogInformation("Using AI Foundry agent '{AgentName}' version '{AgentVersion}' at endpoint '{Endpoint}'",
                _settings.AgentName, _settings.AgentVersion, _settings.ProjectEndpoint);
            var responseClient = projectClient.ProjectOpenAIClient.GetProjectResponsesClientForAgent(agent, null);

            var queryParts = new[] { name, address, city, state, zipCode }
                .Where(s => !string.IsNullOrWhiteSpace(s));
            var locationQuery = string.Join(", ", queryParts);

            var prompt = $"Look up the full location information for: {locationQuery}";

            _logger.LogInformation("AI Foundry prompt: {Prompt}", prompt);

            var response = await responseClient.CreateResponseAsync(prompt, null);

            var outputText = response.Value.GetOutputText();

            _logger.LogDebug("AI Foundry response: {Response}", outputText);

            return ParseAgentResponse(outputText);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "AI Foundry location lookup failed for '{Name}' with address '{Address}', city '{City}', state '{State}', zip '{Zip}'",
                name,
                address,
                city,
                state,
                zipCode);
            _logger.LogWarning("AI lookup failed, falling back to public API lookup.");
            return await _apiFallback.LookupLocationAsync(name, address, city, state, zipCode);
        }
    }

    private static LocationLookupResult ParseAgentResponse(string responseText)
    {
        var jsonText = ExtractJson(responseText);

        using var doc = JsonDocument.Parse(jsonText);
        var root = doc.RootElement;

        return new LocationLookupResult
        {
            Success = true,
            Address = GetString(root, "street_address"),
            City = GetString(root, "city"),
            State = GetString(root, "state"),
            ZipCode = GetString(root, "zipcode"),
            Latitude = GetDouble(root, "latitude"),
            Longitude = GetDouble(root, "longitude"),
        };
    }

    private static string ExtractJson(string text)
    {
        // Agent may wrap JSON in markdown code blocks — extract the raw JSON object
        var jsonStart = text.IndexOf('{');
        var jsonEnd = text.LastIndexOf('}');
        if (jsonStart >= 0 && jsonEnd > jsonStart)
        {
            return text[jsonStart..(jsonEnd + 1)];
        }

        return text;
    }

    private static string GetString(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var val) &&
            val.ValueKind == JsonValueKind.String)
        {
            return val.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    private static double GetDouble(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var val) &&
            val.ValueKind == JsonValueKind.Number)
        {
            return val.GetDouble();
        }

        return 0;
    }
}
