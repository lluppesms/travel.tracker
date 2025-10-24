namespace TravelTracker;

public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyMiddleware> _logger;
    private readonly string? _apiKey;
    private readonly bool _apiKeyRequired;

    public ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<ApiKeyMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _apiKey = configuration["ApiKey"];
        _apiKeyRequired = !string.IsNullOrEmpty(_apiKey);

        if (_apiKeyRequired)
        {
            _logger.LogInformation("API Key validation is enabled");
        }
        else
        {
            _logger.LogInformation("API Key validation is disabled - no API key configured");
        }
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip API key validation
        // - if API key is configured
        // - for things that are not APIs
        // - for health check endpoint
        if (!_apiKeyRequired || !context.Request.Path.StartsWithSegments("/api") || context.Request.Path.StartsWithSegments("/api/healthz"))
        {
            await _next(context);
            return;
        }

        // Check for API key in headers
        if (!context.Request.Headers.TryGetValue("X-API-Key", out var extractedApiKey))
        {
            _logger.LogWarning("API key missing from request to {Path}", context.Request.Path);
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("API Key is required");
            return;
        }

        // Validate API key
        if (!string.Equals(_apiKey, extractedApiKey, StringComparison.Ordinal))
        {
            _logger.LogWarning("Invalid API key provided for request to {Path}", context.Request.Path);
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Invalid API Key");
            return;
        }

        _logger.LogDebug("Valid API key provided for request to {Path}", context.Request.Path);
        await _next(context);
    }
}
