
var builder = WebApplication.CreateBuilder(args);

// Add configuration
builder.Services.Configure<SqlServerSettings>(builder.Configuration.GetSection("SqlServer"));
builder.Services.Configure<AzureAIFoundrySettings>(builder.Configuration.GetSection("AzureAIFoundry"));
var config = builder.Configuration;
// add config to scope
builder.Services.AddSingleton<IConfiguration>(config);

// Add authentication only if Azure AD is configured
var azureAdConfigured = !string.IsNullOrEmpty(builder.Configuration["AzureAd:TenantId"]) &&
                        !string.IsNullOrEmpty(builder.Configuration["AzureAd:ClientId"]);

if (azureAdConfigured)
{
    Console.WriteLine("Azure AD configured - enabling authentication");
    builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApp(options =>
        {
            builder.Configuration.Bind("AzureAd", options);
            options.ResponseType = OpenIdConnectResponseType.Code;
            options.UsePkce = true;
            options.SaveTokens = true;

            if (!options.Scope.Contains("offline_access", StringComparer.OrdinalIgnoreCase))
            {
                options.Scope.Add("offline_access");
            }

            if (!options.Scope.Contains("User.Read", StringComparer.OrdinalIgnoreCase))
            {
                options.Scope.Add("User.Read");
            }
        });

    builder.Services.AddAuthorization(options =>
    {
        options.FallbackPolicy = options.DefaultPolicy;
    });
}
else
{
    Console.WriteLine("Azure AD not configured - running without authentication");
    builder.Services.AddAuthentication();
    builder.Services.AddAuthorization();
}

builder.Services.AddSingleton<DefaultAzureCredential>(provider =>
{
    var creds = new DefaultAzureCredential();
    // for some local development, you need to specify the AD Tenant to make the creds work...
    var visualStudioTenantId = builder.Configuration["VisualStudioTenantId"];
    if (!string.IsNullOrEmpty(visualStudioTenantId))
    {
        Console.WriteLine($"Overwriting tenant for managed identity credentials...");
        creds = new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            ExcludeEnvironmentCredential = true,
            ExcludeManagedIdentityCredential = true,
            TenantId = visualStudioTenantId
        });
    }
    return creds;
});

// Add SQL Server Database Context
var sqlConnectionString = builder.Configuration["SqlServer:ConnectionString"];
if (!string.IsNullOrEmpty(sqlConnectionString))
{
    Console.WriteLine("Connecting to SQL Server database...");
    builder.Services.AddDbContext<TravelTrackerDbContext>(options =>
        options.UseSqlServer(sqlConnectionString));

    // Add repositories
    builder.Services.AddScoped<ILocationRepository, LocationRepository>();
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<INationalParkRepository, NationalParkRepository>();
    builder.Services.AddScoped<ILocationTypeRepository, LocationTypeRepository>();

    // Add services
    builder.Services.AddScoped<ILocationService, LocationService>();
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<INationalParkService, NationalParkService>();
    builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
    builder.Services.AddScoped<IDataImportService, DataImportService>();
    builder.Services.AddScoped<ILocationTypeService, LocationTypeService>();
    builder.Services.AddScoped<IChatbotService, ChatbotService>();

    // Add MCP tools
    builder.Services.AddScoped<LocationTools>();
    builder.Services.AddScoped<NationalParkTools>();
    builder.Services.AddScoped<ChatbotTools>();
}
else
{
    Console.WriteLine("*******  No valid SQL Server configuration found!!!! *******");
    Console.WriteLine("*******  Please configure SqlServer:ConnectionString *******");
}
// Add Razor Pages for authentication
if (azureAdConfigured)
{
    builder.Services.AddRazorPages()
        .AddMicrosoftIdentityUI();
}
else
{
    builder.Services.AddRazorPages();
}

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped(sp =>
{
    var nav = sp.GetRequiredService<NavigationManager>();
    return new HttpClient { BaseAddress = new Uri(nav.BaseUri) };
});

// Add API controllers
builder.Services.AddControllers();

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Travel Tracker API",
        Version = "v1",
        Description = "API for managing travel locations, national parks, and location types. Designed for MCP protocol integration and Agent Framework usage.",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Travel Tracker",
            Url = new Uri("https://github.com/lluppesms/travel.tracker")
        }
    });

    // Include XML comments for better API documentation
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    // API Key header security scheme
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "X-API-Key",
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Description = "API Key required for secured endpoints",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "ApiKeyAuth"
        }
    };
    options.AddSecurityDefinition("ApiKeyAuth", securityScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            { securityScheme, Array.Empty<string>() }
        });

});

// Add Model Context Protocol (MCP) Server
if (!string.IsNullOrEmpty(sqlConnectionString))
{
    Console.WriteLine("Configuring MCP server...");
    builder.Services.AddMcpServer(options =>
    {
        options.ServerInfo = new()
        {
            Name = "Travel Tracker MCP Server",
            Version = "1.0.0"
        };
    }).WithHttpTransport()
      .WithToolsFromAssembly();
}

// Add HTTP context accessor for getting authenticated user
builder.Services.AddHttpContextAccessor();
builder.Services.AddCascadingAuthenticationState();

// Add Weather Tool for MCP Servers
builder.Services.AddHttpClient("WeatherAPI", client =>
{
    client.BaseAddress = new Uri("https://api.weather.gov/");
    client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("mcp-weather-server", "1.0"));
});

// --------------------------------------------------------------------------------------------------------------------------------------------
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// Enable Swagger middleware
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Travel Tracker API v1");
    options.RoutePrefix = "api/swagger";
    options.DocumentTitle = "Travel Tracker API Documentation";
    options.DisplayRequestDuration();
});

// Add API key middleware
app.UseMiddleware<TravelTracker.ApiKeyMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorPages();
app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Map MCP endpoint
if (!string.IsNullOrEmpty(builder.Configuration["SqlServer:ConnectionString"]))
{
    app.MapMcp("/api/mcp");
    Console.WriteLine("MCP server endpoint configured at /api/mcp");
}

app.Run();
