namespace TravelTracker.Mcp.DependencyInjection;

public static class McpServiceCollectionExtensions
{
    public static IServiceCollection AddTravelTrackerMcpDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IConfiguration>(configuration);

        services.AddHttpContextAccessor();
        services.AddScoped<HttpContextAccessor>();

        // MCP server is configured as API-first and does not require direct SQL access.
        services.AddScoped<IAuthenticationService, McpPassthroughAuthenticationService>();

        services.AddScoped<LocationTools>();

        services.AddHttpClient("WeatherAPI", client =>
        {
            client.BaseAddress = new Uri("https://api.weather.gov/");
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("mcp-weather-server", "1.0"));
        });

        var locationApiEndpoint = configuration["LocationApiEndpoint"];
        if (string.IsNullOrWhiteSpace(locationApiEndpoint))
        {
            throw new InvalidOperationException("Missing required LocationApiEndpoint configuration for MCP location tools.");
        }

        services.AddHttpClient("TravelTrackerLocationsApi", client =>
        {
            client.BaseAddress = new Uri($"{locationApiEndpoint.TrimEnd('/')}/");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });

        return services;
    }
}
