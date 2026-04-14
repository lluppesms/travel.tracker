# Travel Tracker MCP

Model Context Protocol (MCP) servers for the Travel Tracker solution. This folder contains:

- A streamable HTTP/SSE MCP host (`TravelTracker.MCP.Http`)
- A stdio MCP host (`TravelTracker.MCP.Stdio`)
- Shared MCP tool implementations (`TravelTracker.MCP.Tools`)

The MCP servers are built on .NET 10 and `ModelContextProtocol` v0.4.0-preview.3.

## What This Server Exposes

The current MCP tools are implemented in `TravelTracker.MCP.Tools/Mcp`:

- `get_current_time`
- `get_weather_forecast`
- `get_all_locations`
- `get_location_by_id`
- `get_locations_by_state`
- `get_locations_by_date_range`
- `get_location_count_by_state`

## Project Layout

- `TravelTracker.MCP.Http`: ASP.NET Core MCP server using streamable HTTP/SSE transport (`/mcp` route)
- `TravelTracker.MCP.Stdio`: Console MCP server using stdio transport
- `TravelTracker.MCP.Tools`: Shared tool classes, data/services wiring, and dependency injection setup

## Prerequisites

- .NET 10 SDK
- Valid Travel Tracker API key and user configuration for location tools
- Optional: Azure AI Foundry settings if you plan to use chatbot functionality

## Required Configuration

Both hosts read settings from their local `appsettings.json` files and environment variables.

Required keys:

- `LocationApiEndpoint` (example: `https://localhost:7134/api/locations`)
- `ApiKey` (used as `X-API-Key` when calling location APIs)
- `ApiKey_UserID` (required by stdio auth flow)

Additional chatbot-related keys are under `AzureAIFoundry`.

Authentication behavior in MCP mode:

- MCP tools use API-first pass-through validation.
- Any `userId <= 0` is rejected locally.
- For non-zero user IDs, downstream URL-backed APIs perform authorization checks.

## Run Locally

From `src/TravelTracker.MCP`:

```powershell
dotnet build
```

### HTTP/SSE transport

```powershell
dotnet run --project .\TravelTracker.MCP.Http\TravelTracker.MCP.Http.csproj
```

Server info endpoint:

- `GET /` returns service metadata

MCP endpoint:

- `/mcp`

### STDIO transport

```powershell
dotnet run --project .\TravelTracker.MCP.Stdio\TravelTracker.MCP.Stdio.csproj
```

Use this mode for MCP clients that launch a local process and communicate over stdin/stdout.

## mcp.json Configuration Examples

Use one of the following `mcp.json` patterns depending on how you want to host/run MCP.

### 1. Hosted website URL (HTTP MCP endpoint)

Use this when your MCP server is already deployed (for example, Azure App Service) and exposed at an HTTPS endpoint.

```json
{
    "servers": {
        "travel-tracker-hosted": {
            "type": "http",
            "url": "https://your-app-name.azurewebsites.net/mcp",
            "headers": {
                "X-API-Key": "YOUR_API_KEY"
            }
        }
    }
}
```

If your deployed app uses `/api/mcp` instead of `/mcp`, update the `url` accordingly.

### 2. Run locally from source (STDIO)

Use this for local development when your MCP client should launch the .NET project directly.

```json
{
    "servers": {
        "travel-tracker-local": {
            "command": "dotnet",
            "args": [
                "run",
                "--project",
                "c:/Projects/GHCP/travels/travel.tracker/src/TravelTracker.MCP/TravelTracker.MCP.Stdio/TravelTracker.MCP.Stdio.csproj"
            ],
            "env": {
                "LocationApiEndpoint": "https://localhost:7134/api/locations",
                "ApiKey": "YOUR_API_KEY",
                "ApiKey_UserID": "1"
            }
        }
    }
}
```

### 3. Run with Docker (STDIO in container)

Use this when your MCP client should start a containerized MCP process.

```json
{
    "servers": {
        "travel-tracker-docker": {
            "command": "docker",
            "args": [
                "run",
                "-i",
                "--rm",
                "travel-tracker-mcp-stdio:local"
            ],
            "env": {
                "LocationApiEndpoint": "https://host.docker.internal:7134/api/locations",
                "ApiKey": "YOUR_API_KEY",
                "ApiKey_UserID": "1"
            }
        }
    }
}
```

Build the local image name used above (`travel-tracker-mcp-stdio:local`) from your Dockerfile before using this entry.

Example Dockerfile for the stdio MCP host:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .
RUN dotnet publish ./src/TravelTracker.MCP/TravelTracker.MCP.Stdio/TravelTracker.MCP.Stdio.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "TravelTracker.MCP.Stdio.dll"]
```

Build command example (run from repo root):

```powershell
docker build -t travel-tracker-mcp-stdio:local -f Dockerfile .
```

> Tip: exact MCP JSON shape varies slightly by client. If your client does not support `type` or `headers` fields, keep the same values but adapt to that client's schema.

## Notes

- `WeatherTools` uses `https://api.weather.gov/` and expects U.S./territory coordinates.
- Location tools require successful user validation through configured API key + user ID context.
- HTTP host maps MCP as anonymous at `/mcp`; enforce perimeter security appropriately when exposing beyond local development.

## Related Documentation

- `Docs/MCP-SETUP.md`
- `Docs/API-Documentation.md`

## License

This repository is licensed under the terms in `LICENSE`.
