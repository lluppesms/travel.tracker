# Development Status

**Last reviewed:** 2026-09-04

This page is the working record for implementation progress. The [root README](../README.md) explains the product and architecture; the [project map](../MAP.md) is the authoritative technical guide for maintainers.

## Current Snapshot

Travel Tracker is a .NET 10 Blazor application backed by SQL Server. The core web experience, REST API, SQL project, Bicep infrastructure, GitHub Actions workflows, MCP hosts, and a confirmation-only travel assistant are implemented in the repository.

The assistant supports location searches, location-type lookup, place lookup, and preparing a visited-location entry. Writes require a user-facing confirmation step and are durably tracked for safe retries.

## Implemented Areas

- Travel locations, destination and location types, import/export, maps, and statistics through the Blazor web application.
- REST endpoints, Swagger, API-key handling, and optional Microsoft Entra ID authentication.
- EF Core repositories and the `Travel` SQL Server schema, managed through the SQL database project and DACPAC.
- Azure Maps and Azure AI Foundry integration points, with a disabled-but-safe assistant experience when its prerequisites are not configured.
- MCP tool hosts for HTTP and standard input/output transports.
- Bicep composition for the App Service, SQL, Key Vault, storage, monitoring, SignalR, managed identity, Azure Maps, and assistant-related configuration.
- GitHub Actions and Azure DevOps pipeline definitions for build, test, deployment, scanning, and database operations.

## Validation

The repository includes xUnit tests under `src/TravelTracker.Tests/`. Run the focused suite with:

```powershell
dotnet test .\src\TravelTracker.Tests\TravelTracker.Tests.csproj
```

## Tracking Notes

- Current code, project files, startup configuration, SQL objects, and workflow YAML take precedence over historical planning documents.
- The original plan and phase reports are retained for context and may describe superseded choices, including earlier Cosmos DB-based designs.
- Keep this page to concise, dated milestones. Update [MAP.md](../MAP.md) when an architectural decision, configuration key, workflow, or source location changes.
