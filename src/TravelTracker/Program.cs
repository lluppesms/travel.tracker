using Azure.Identity;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Azure.Cosmos;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using TravelTracker.Components;
using TravelTracker.Data.Configuration;
using TravelTracker.Data.Repositories;
using TravelTracker.Services.Interfaces;
using TravelTracker.Services.Services;

var builder = WebApplication.CreateBuilder(args);

// Add configuration
builder.Services.Configure<CosmosDbSettings>(builder.Configuration.GetSection("CosmosDb"));

// Add authentication
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

// Add Cosmos DB client
// var cosmosConnectionString = builder.Configuration["CosmosDb:ConnectionString"];
var cosmosEndpoint = builder.Configuration["CosmosDb:Endpoint"];
var visualStudioTenantId = builder.Configuration["VisualStudioTenantId"];
var cosmosClientOptions = new CosmosClientOptions
{
    MaxRetryAttemptsOnRateLimitedRequests = 9,
    MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(30),
    RequestTimeout = TimeSpan.FromSeconds(10),
    ConnectionMode = ConnectionMode.Gateway
};
//if (!string.IsNullOrEmpty(cosmosConnectionString))
//{
//builder.Services.AddSingleton<CosmosClient>(serviceProvider =>
//{
//    return new CosmosClient(cosmosConnectionString);
//});
if (!string.IsNullOrEmpty(cosmosEndpoint))
{
    Console.WriteLine($"Connecting to Cosmos endpoint {cosmosEndpoint} with managed identity...");
    Console.WriteLine($"Tenant for managed identity {visualStudioTenantId}...");
    builder.Services.AddSingleton<CosmosClient>(provider =>
    {
        var creds = new DefaultAzureCredential();
        // for some local development, you need to specify the AD Tenant to make the creds work...
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
        var cosmosClient = new CosmosClient(cosmosEndpoint, creds, cosmosClientOptions);
        return cosmosClient;
    });

    // Add repositories
    builder.Services.AddScoped<ILocationRepository, LocationRepository>();
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<INationalParkRepository, NationalParkRepository>();

    // Add services
    builder.Services.AddScoped<ILocationService, LocationService>();
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<INationalParkService, NationalParkService>();
    builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
}
else
{
    // Add Cosmos DB services based on whether connection string exists
    Console.WriteLine("No Cosmos Endpoint Found... Looking for Cosmos DB connection string...");
    var connectionString = builder.Configuration["CosmosDb:ConnectionString"];
    if (!string.IsNullOrEmpty(connectionString))
    {
        var accountName = connectionString?[..connectionString.IndexOf("AccountKey")].Replace("AccountEndpoint=https://", "").Replace(".documents.azure.com:443/;", "").Replace("/;", "");
        Console.WriteLine($"Connecting to Cosmos DB Account {accountName} with a key...");
        builder.Services.AddSingleton<CosmosClient>(provider =>
        {
            var cosmosClient = new CosmosClient(connectionString, cosmosClientOptions);
            return cosmosClient;
        });

        // Add repositories
        builder.Services.AddScoped<ILocationRepository, LocationRepository>();
        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<INationalParkRepository, NationalParkRepository>();

        // Add services
        builder.Services.AddScoped<ILocationService, LocationService>();
        builder.Services.AddScoped<IUserService, UserService>();
        builder.Services.AddScoped<INationalParkService, NationalParkService>();
        builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
    }
    else
    {
        Console.WriteLine("*******  No valid Cosmos DB configuration found!!!! *******");
        Console.WriteLine("*******         Using MOCK Cosmos Service!!!!       *******");
        // services.AddScoped<ICosmosDbService, MockCosmosDbService>();
    }
}
//}
// Add Razor Pages for authentication
builder.Services.AddRazorPages()
    .AddMicrosoftIdentityUI();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add HTTP context accessor for getting authenticated user
builder.Services.AddHttpContextAccessor();
builder.Services.AddCascadingAuthenticationState();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorPages();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
