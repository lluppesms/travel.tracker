using TravelTracker.Mcp;
using TravelTracker.Mcp.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTravelTrackerMcpDependencies(builder.Configuration);

builder.Services.AddMcpServer(options =>
{
    options.ServerInfo = new()
    {
        Name = "Travel Tracker MCP Server (HTTP)",
        Version = "1.0.0"
    };
})
.WithHttpTransport(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.MaxIdleSessionCount = 500;
})
.WithToolsFromAssembly(typeof(TimeTools).Assembly, serializerOptions: null);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.MapGet("/", () => Results.Ok(new
{
    service = "Travel Tracker MCP HTTP Server",
    endpoint = "/mcp",
    transport = "Streamable HTTP/SSE"
}));

app.MapMcp("/mcp").AllowAnonymous();

app.Run();
