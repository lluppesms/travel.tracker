using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TravelTracker.Mcp;
using TravelTracker.Mcp.DependencyInjection;
using TravelTracker.Mcp.Stdio;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.AddTravelTrackerMcpDependencies(builder.Configuration);
builder.Services.AddScoped<IAuthenticationService, StdioAuthenticationService>();

builder.Services.AddMcpServer(options =>
{
    options.ServerInfo = new()
    {
        Name = "Travel Tracker MCP Server (STDIO)",
        Version = "1.0.0"
    };
})
.WithStdioServerTransport()
.WithToolsFromAssembly(typeof(TimeTools).Assembly, serializerOptions: null);

await builder.Build().RunAsync();
