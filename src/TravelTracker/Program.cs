extern alias AzureIdentity;

using TravelTracker.Helpers;
using TravelTracker.Services;
using TravelTracker.Authentication;
using TravelTracker.Data;
using TravelTracker.Extensions;
using TravelTracker.Helpers;
using TravelTracker.Services;
using TravelTracker.Services.Configuration;
using TravelTracker.Services.Interfaces;
using TravelTracker.Services.Services;
using Microsoft.AspNetCore.DataProtection;
using DefaultAzureCredential = AzureIdentity::Azure.Identity.DefaultAzureCredential;
using DefaultAzureCredentialOptions = AzureIdentity::Azure.Identity.DefaultAzureCredentialOptions;

var builder = WebApplication.CreateBuilder(args);

// Validate DI lifetimes at build time so a singleton can never capture a scoped service.
// Enabled in tests and development (TASK-004); production keeps the default provider behavior.
if (!builder.Environment.IsProduction())
{
    builder.Host.UseDefaultServiceProvider((context, options) =>
    {
        options.ValidateScopes = true;
        options.ValidateOnBuild = true;
    });
}

// Add configuration
builder.Services.Configure<SqlServerSettings>(builder.Configuration.GetSection("SqlServer"));
builder.Services.Configure<AzureAIFoundrySettings>(builder.Configuration.GetSection("AzureAIFoundry"));
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.AddScoped<IRelativeDateResolver, RelativeDateResolver>();
var dataProtectionBuilder = builder.Services.AddDataProtection()
    .SetApplicationName("TravelTracker");
var dataProtectionKeysPath = builder.Configuration["TravelAssistant:DataProtectionKeysPath"];
var localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
dataProtectionKeysPath = string.IsNullOrWhiteSpace(dataProtectionKeysPath)
    ? Path.Combine(
        string.IsNullOrWhiteSpace(localApplicationData)
            ? builder.Environment.ContentRootPath
            : localApplicationData,
        "TravelTracker",
        "DataProtection-Keys")
    : dataProtectionKeysPath;
dataProtectionBuilder.PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));
builder.Services.AddSingleton<IPlaceCandidateStore, PlaceCandidateStore>();
builder.Services.AddSingleton<IPlaceLookupRateLimiter, PlaceLookupRateLimiter>();
builder.Services.AddSingleton<ICopilotRuntimeAccessor, CopilotRuntimeAccessor>();
builder.Services.AddSingleton<ICopilotTravelToolFactory, CopilotTravelToolFactory>();
var config = builder.Configuration;
// add config to scope
builder.Services.AddSingleton<IConfiguration>(config);

// Travel assistant registration (REQ-002, OPS-008, SEC-004).
// The application deliberately keeps running locally when SQL Server or Azure AD are absent (see the
// warnings printed further below), so those prerequisites are enforced for the ASSISTANT SURFACE ONLY.
// When the prerequisites are satisfied the TravelAssistant options are validated with ValidateOnStart,
// so AutoExecute, an unknown provider, or incomplete provider settings fail startup. When they are not,
// the assistant is not registered and startup reports the missing configuration KEY names only.
var assistantPrerequisiteFailures = ChatProviderServiceCollectionExtensions.GetAssistantPrerequisiteFailures(builder.Configuration);
var travelAssistantEnabled = assistantPrerequisiteFailures.Count == 0;

// Single source of truth for the SQL connection string, shared with the assistant prerequisite check.
var sqlConnectionString = AssistantConnectionStrings.Resolve(builder.Configuration);
var sqlConfigured = !string.IsNullOrWhiteSpace(sqlConnectionString);

if (travelAssistantEnabled)
{
    builder.Services.AddTravelAssistantOptions(builder.Configuration);
}
else
{
    builder.Services.Configure<TravelAssistantOptions>(builder.Configuration.GetSection(TravelAssistantOptions.SectionName));
    Console.WriteLine("*******  Travel assistant disabled - required configuration is missing: *******");
    foreach (var failure in assistantPrerequisiteFailures)
    {
        Console.WriteLine($"*******  {failure}");
    }
}

builder.Services.AddTravelAssistantReadiness(travelAssistantEnabled, assistantPrerequisiteFailures);

// Health check service for readiness probes (TASK-015)
builder.Services.AddScoped<ICopilotHealthCheckService, CopilotHealthCheckService>();

// Session coordinator for Copilot SDK integration (TASK-016).
builder.Services.AddSingleton<ICopilotSessionCoordinator, CopilotSessionCoordinator>();
builder.Services.AddHostedService<CopilotRuntimeHostedService>();

// CurrentTravelUserResolver depends on IUserService, which only exists when SQL is configured.
if (sqlConfigured)
{
    builder.Services.AddTravelAssistantIdentity();
}
else
{
    builder.Services.AddUnavailableTravelAssistantIdentity();
}

// Add authentication only if Azure AD is configured
var azureAdConfigured = !string.IsNullOrWhiteSpace(builder.Configuration[TravelAssistantOptionsValidator.AzureAdTenantIdKey]) &&
                        !string.IsNullOrWhiteSpace(builder.Configuration[TravelAssistantOptionsValidator.AzureAdClientIdKey]);
if (azureAdConfigured)
{
    // Personal Laptop
    // Use PKCE (no client_secret required) — requires "Allow public client flows" = Yes
    // in Azure Portal: App Registration > Authentication > Advanced Settings
    builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

    // Testing Laptop
    // Console.WriteLine("Azure AD configured - enabling authentication");
    // builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    //     .AddMicrosoftIdentityWebApp(options =>
    //     {
    //         builder.Configuration.Bind("AzureAd", options);
    //         options.ResponseType = OpenIdConnectResponseType.Code;
    //         options.UsePkce = true;
    //         options.SaveTokens = true;
    // 
    //        if (!options.Scope.Contains("offline_access", StringComparer.OrdinalIgnoreCase))
    //        {
    //             options.Scope.Add("offline_access");
    //         }
    // 
    //         if (!options.Scope.Contains("User.Read", StringComparer.OrdinalIgnoreCase))
    //         {
    //             options.Scope.Add("User.Read");
    //         }
    //     });

    builder.Services.AddAuthorization(options =>
    {
        // options.FallbackPolicy = options.DefaultPolicy;
        options.FallbackPolicy = null; // Don't force auth globally — individual pages use [Authorize]
    });
}
else
{
    Console.WriteLine("Azure AD not configured - running without authentication");
    builder.Services.AddAuthentication(UnconfiguredAuthenticationHandler.SchemeName)
        .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, UnconfiguredAuthenticationHandler>(UnconfiguredAuthenticationHandler.SchemeName, null);
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
            ExcludeEnvironmentCredential = false,
            ExcludeManagedIdentityCredential = false,
            TenantId = visualStudioTenantId
        });
    }
    else
    {
        Console.WriteLine($"Using default tenant for managed identity credentials...");
        creds = new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            ExcludeEnvironmentCredential = false,
            ExcludeManagedIdentityCredential = false
        });
    }
    return creds;
});

// Add SQL Server Database Context
if (sqlConfigured)
{
    var sqlConnectionObject = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(sqlConnectionString);
    var dataSource = $"SQL Server: {sqlConnectionObject.DataSource}, Database: {sqlConnectionObject.InitialCatalog}";
    Console.WriteLine($"Connecting to {dataSource}...");
    builder.Services.AddDbContext<TravelTrackerDbContext>(options => options.UseSqlServer(
        sqlConnectionString,
        sqlOptions => sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", DatabaseSchema.Name)));

    // Add repositories
    builder.Services.AddScoped<ILocationRepository, LocationRepository>();
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<ILocationTypeRepository, LocationTypeRepository>();
    builder.Services.AddScoped<IDestinationRepository, DestinationRepository>();
    builder.Services.AddScoped<IDestinationTypeRepository, DestinationTypeRepository>();
    builder.Services.AddScoped<IAssistantActionRepository, AssistantActionRepository>();
    builder.Services.AddScoped<ILocationSummaryRepository, LocationSummaryRepository>();

    // Add services
    builder.Services.AddScoped<ILocationService, LocationService>();
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
    builder.Services.AddScoped<IDataImportService, DataImportService>();
    builder.Services.AddScoped<IDataExportService, DataExportService>();
    builder.Services.AddScoped<ILocationTypeService, LocationTypeService>();
    builder.Services.AddScoped<IDestinationService, DestinationService>();
    builder.Services.AddScoped<ITravelAssistantActionService, TravelAssistantActionService>();
    builder.Services.AddScoped<ITravelAssistantActionConfirmationService, TravelAssistantActionConfirmationService>();
    builder.Services.AddHostedService<AssistantActionCleanupHostedService>();

    // Register LocationLookupAPIService (public API fallback) with HttpClient
    builder.Services.AddHttpClient<LocationLookupAPIService>(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(15);
    });

    // Register LocationLookupService (uses Azure AI Foundry agent with API fallback)
    builder.Services.AddScoped<ILocationLookupService, LocationLookupService>();

    // Add build info service
    builder.Services.AddScoped<IBuildInfoService, BuildInfoService>();
}
else
{
    Console.WriteLine("*******  No valid SQL Server configuration found!!!! *******");
    Console.WriteLine("*******  Please configure SqlServer:ConnectionString *******");
}

// Provider selection happens in one place; Phase 3 adds CopilotChatbotService there.
if (travelAssistantEnabled)
{
    builder.Services.AddTravelAssistantChatProvider(builder.Configuration);
}
else
{
    builder.Services.AddDisabledTravelAssistantChatProvider();
}

if (!string.IsNullOrWhiteSpace(builder.Configuration["AzureAIFoundry:Endpoint"]))
{
    Console.WriteLine("Azure AI Foundry configured (AzureAIFoundry:Endpoint, AzureAIFoundry:DeploymentName, AzureAIFoundry:ApiKey)...");
}
else
{
    Console.WriteLine("*******  No valid Azure AI Foundry configuration found!!!! *******");
    Console.WriteLine("*******  Please configure AzureAIFoundry:Endpoint, AzureAIFoundry:ApiKey, and AzureAIFoundry:DeploymentName *******");
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
    options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "Travel Tracker API",
        Version = "v1",
        Description = "API for managing travel locations, national parks, and location types. Designed for MCP protocol integration and Agent Framework usage.",
        Contact = new Microsoft.OpenApi.OpenApiContact
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
        //Reference = new OpenApiReference
        //{
        //    Type = ReferenceType.SecurityScheme,
        //    Id = "ApiKeyAuth"
        //}
    };
    options.AddSecurityDefinition("ApiKeyAuth", securityScheme);
    //options.AddSecurityRequirement(new OpenApiSecurityRequirement
    //    {
    //        { securityScheme, Array.Empty<string>() }
    //    });
});

// Add HTTP context accessor for getting authenticated user
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<HttpContextAccessor>();

builder.Services.AddCascadingAuthenticationState();

builder.Services.AddTransient<Microsoft.AspNetCore.Authentication.IClaimsTransformation, MyClaimsTransformation>();

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

// Login/logout endpoints (redirect-based, compatible with Blazor Server)
app.MapGet("/account/login", (string? returnUrl) =>
{
    var props = new Microsoft.AspNetCore.Authentication.AuthenticationProperties
    {
        RedirectUri = string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl
    };
    return Results.Challenge(props, [OpenIdConnectDefaults.AuthenticationScheme]);
});

app.MapPost("/account/logout", async (HttpContext ctx) =>
{
    await Microsoft.AspNetCore.Authentication.AuthenticationHttpContextExtensions.SignOutAsync(ctx, CookieAuthenticationDefaults.AuthenticationScheme);
    await Microsoft.AspNetCore.Authentication.AuthenticationHttpContextExtensions.SignOutAsync(ctx, OpenIdConnectDefaults.AuthenticationScheme);
    return Results.LocalRedirect("/");
}).RequireAuthorization();

app.MapStaticAssets();
app.MapRazorPages();
app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
