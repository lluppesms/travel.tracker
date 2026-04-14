namespace TravelTracker.Mcp.DependencyInjection;

public static class McpServiceCollectionExtensions
{
    public static IServiceCollection AddTravelTrackerMcpDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SqlServerSettings>(configuration.GetSection("SqlServer"));
        services.Configure<AzureAIFoundrySettings>(configuration.GetSection("AzureAIFoundry"));
        services.AddSingleton<IConfiguration>(configuration);

        services.AddHttpContextAccessor();
        services.AddScoped<HttpContextAccessor>();

        var sqlConnectionString = configuration["SqlServer:ConnectionString"];
        if (string.IsNullOrWhiteSpace(sqlConnectionString))
        {
            throw new InvalidOperationException("Missing required SqlServer:ConnectionString configuration for MCP services.");
        }

        services.AddDbContext<TravelTrackerDbContext>(options => options.UseSqlServer(sqlConnectionString));

        services.AddScoped<ILocationRepository, LocationRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ILocationTypeRepository, LocationTypeRepository>();
        services.AddScoped<IDestinationRepository, DestinationRepository>();
        services.AddScoped<IDestinationTypeRepository, DestinationTypeRepository>();

        services.AddScoped<ILocationService, LocationService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<ILocationTypeService, LocationTypeService>();
        services.AddScoped<IChatbotService, ChatbotService>();
        services.AddScoped<IDestinationService, DestinationService>();

        services.AddScoped<LocationTools>();
        services.AddScoped<ChatbotTools>();

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
