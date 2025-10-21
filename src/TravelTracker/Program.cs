using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using TravelTracker.Components;
using TravelTracker.Data;
using TravelTracker.Data.Configuration;
using TravelTracker.Data.Repositories;
using TravelTracker.Services.Interfaces;
using TravelTracker.Services.Services;

var builder = WebApplication.CreateBuilder(args);

// Add configuration
builder.Services.Configure<SqlServerSettings>(builder.Configuration.GetSection("SqlServer"));

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

    // Add services
    builder.Services.AddScoped<ILocationService, LocationService>();
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<INationalParkService, NationalParkService>();
    builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
    builder.Services.AddScoped<IDataImportService, DataImportService>();
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
