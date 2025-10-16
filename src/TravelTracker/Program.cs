using Microsoft.Azure.Cosmos;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
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
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddAuthorization();

// Add Cosmos DB client
var cosmosConnectionString = builder.Configuration["CosmosDb:ConnectionString"];
if (!string.IsNullOrEmpty(cosmosConnectionString))
{
    builder.Services.AddSingleton<CosmosClient>(serviceProvider =>
    {
        return new CosmosClient(cosmosConnectionString);
    });
}

// Add repositories
builder.Services.AddScoped<ILocationRepository, LocationRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<INationalParkRepository, NationalParkRepository>();

// Add services
builder.Services.AddScoped<ILocationService, LocationService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<INationalParkService, NationalParkService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

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
