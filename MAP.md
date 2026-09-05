# Travel Tracker Project Map

> **Status:** Living project document. Update this file whenever the repository's architecture, behavior, configuration, testing, automation, or Copilot customization changes.
>
> **Purpose:** Give Copilot a fast, evidence-based map of this repository so routine work starts at the owning project instead of scanning the whole tree.
>
> **Last reviewed:** 2026-09-05

## 1. Project Identity

Travel Tracker is a .NET 10 personal travel tracking and visualization application. The Blazor web app records locations and destinations, displays travel data, imports and exports data, exposes REST APIs, and provides an AI chat experience. Separate MCP hosts expose travel tools to compatible AI clients.

The application is under active development. Treat current project files, startup code, and database objects as authoritative when older planning reports disagree with the implementation.

Authoritative sources, in order:

1. Current project files, `Program.cs`, source code, SQL objects, and workflow YAML.
2. `README.md` and focused documentation under `Docs/`.
3. This `MAP.md` document.
4. Historical planning and phase reports under `reports/`.

## 2. Fast Routing Guide

| Task | Start here | Then inspect |
| --- | --- | --- |
| Change a Blazor page or component | `src/TravelTracker/Components/` | `src/TravelTracker/Program.cs`, related component code and CSS |
| Change REST API behavior | `src/TravelTracker/Controllers/` | services, repositories, `ApiKeyMiddleware.cs`, Swagger setup in `Program.cs` |
| Change persistence or queries | `src/TravelTracker.Data/Repositories/` | `TravelTrackerDbContext.cs`, models, `DatabaseSchema.cs` |
| Change business logic | `src/TravelTracker.Services/` | service interfaces, data repositories, related tests |
| Change authentication | `src/TravelTracker/Program.cs` and `Services/AuthenticationService.cs` | `Helpers/MyClaimsTransformation.cs`, `AzureAd` configuration |
| Change AI chat or location lookup | `src/TravelTracker.Services/Services/` | `AzureAIFoundry` settings, controllers, chat UI, related tests |
| Change MCP tools or transport | `src/TravelTracker.MCP/TravelTracker.MCP.Tools/` | HTTP and Stdio host `Program.cs` files and MCP README |
| Change the database schema | `src/sql.database/Travel/` | project file, pre/post-deployment scripts, SQL instructions |
| Change Azure resources | `infra/Bicep/main.bicep` and `infra/Bicep/modules/` | `main.bicepparam`, resource names, pipeline variables |
| Change GitHub deployment | `.github/workflows/` | called `template-*.yml` workflows and `.github/workflows-readme.md` |
| Change Azure DevOps deployment | `.azdo/pipelines/` | `pipes/` templates and `vars/` |
| Add or repair tests | `src/TravelTracker.Tests/` | production controller/service/repository and testing instructions |
| Change Copilot behavior | `.github/copilot-instructions.md` and `.github/instructions/` | `.github/skills/`, `.github/agents/`, `.github/prompts/`, this file |

## 3. Architecture and Data Flow

### Web application

`src/TravelTracker/` is an ASP.NET Core Blazor Web App targeting `net10.0` with interactive server components. `Program.cs` owns configuration, dependency injection, authentication, EF Core registration, API controllers, Swagger, middleware, static assets, Razor Pages, and component routing.

The normal request path is:

1. A browser connects to the interactive server-rendered Blazor app.
2. Components call services from `TravelTracker.Services`.
3. Services use repository interfaces from `TravelTracker.Data`.
4. Repositories use `TravelTrackerDbContext` and SQL Server.
5. Controllers expose locations, location types, destinations, chatbot, and configuration endpoints over the same service layer.
6. `ApiKeyMiddleware` applies API-key handling to HTTP API traffic.

The Map page initializes the Azure Maps Web SDK through `wwwroot/js/azureMaps.js`. In addition to SDK readiness, it monitors Azure Maps error events and checks that basemap tile requests occur shortly after initialization. A detected issue writes detailed context to the browser console, logs a structured server warning through `MapView`, and presents a dismissible user warning while preserving location pins. A browser `Failed to fetch` error also directs the user to verify the Maps key or RBAC roles and Azure Maps CORS definitions.

Microsoft Entra ID authentication is enabled only when both `AzureAd:TenantId` and `AzureAd:ClientId` are configured. Otherwise, the app starts without an authenticated fallback policy. Swagger is served at `/api/swagger`.

The SQL-backed repositories and application services are registered only when `SqlServer:ConnectionString` is non-empty. There is no implemented JSON repository fallback in the current web startup path.

The travel assistant surface is gated separately from the rest of the app. `Program.cs` enables `ValidateScopes` and `ValidateOnBuild` outside Production, and uses `ChatProviderServiceCollectionExtensions` (`src/TravelTracker/Extensions/`) to check assistant prerequisites (`AzureAd:TenantId`, `AzureAd:ClientId`, and the SQL connection string resolved by `AssistantConnectionStrings.Resolve`, which accepts `SqlServer:ConnectionString` or `ConnectionStrings:DefaultConnection`). When they are satisfied, `AddTravelAssistantOptions` validates the `TravelAssistant` configuration section at startup and `AddTravelAssistantChatProvider` selects the chat provider from `TravelAssistant:Provider` (`AgentFramework` today; `CopilotSDK` fails fast until it ships). When they are missing, a key-only warning is written, `DisabledChatbotService` is registered so the assistant reports `provider_unavailable` instead of a dependency injection error, and unrelated pages continue to run. `ICurrentPrincipalAccessor` and `ICurrentTravelUserResolver` are always registered as scoped services, using `UnavailableTravelUserResolver` when SQL is absent because `IUserService` does not exist on that path. Assistant entry points check `TravelAssistantReadiness` before identity, so a disabled assistant returns `provider_unavailable` rather than a misleading authentication failure. When Entra ID is not configured, `UnconfiguredAuthenticationHandler` (`src/TravelTracker/Authentication/`) is the default scheme and returns a plain `401` instead of an unhandled challenge exception.

### Data and services

`src/TravelTracker.Data/` contains the EF Core context, configuration classes, domain models, repository interfaces, and SQL repository implementations. `TravelTrackerDbContext` exposes users, locations, location types, destinations, and destination types. Database objects use the `Travel` schema.

`src/TravelTracker.Services/` contains business services for authentication, users, locations, location types, destinations, import/export, chatbot behavior, and location lookup. `IRelativeDateResolver` provides server-authoritative `today` and `yesterday` resolution using the registered `TimeProvider` and configured `TravelAssistant:TimeZoneId`; unsupported expressions require clarification and model-proposed dates must agree. AI and lookup services can call Azure AI Foundry and public geocoding APIs, depending on configuration.

### MCP hosts

`src/TravelTracker.MCP/` contains three projects:

- `TravelTracker.MCP.Tools`: shared MCP tool implementations and models.
- `TravelTracker.MCP.Http`: HTTP MCP host with health and MCP endpoints.
- `TravelTracker.MCP.Stdio`: standard-input/output host for local MCP clients.

Read `src/TravelTracker.MCP/README.md` and each host's startup code before changing transport, configuration, or tool registration.

## 4. Repository Layout

```text
src/
  TravelTracker/             Blazor web host, components, controllers, helpers, web services
  TravelTracker.Data/        EF Core context, models, configuration, repositories
  TravelTracker.Services/    Application service interfaces and implementations
  TravelTracker.Tests/       xUnit controller, data, and service tests
  TravelTracker.MCP/
    TravelTracker.MCP.Tools/ Shared MCP tools and models
    TravelTracker.MCP.Http/  HTTP MCP host
    TravelTracker.MCP.Stdio/ Stdio MCP host
  sql.database/              SQL project and Travel schema objects
  TravelTracker.sln          Main .NET solution
  requests-*.http            Local API and MCP request collections

infra/
  Bicep/                     Main Bicep composition, modules, parameters, deployment scripts
  azd-main.bicep             Azure Developer CLI entry point

.github/
  copilot-instructions.md    Repository-wide Copilot policy
  instructions/              Focused authoring and review instructions
  skills/                    Reusable repository skills
  agents/                    Custom agents
  prompts/                   Task prompts
  workflows/                 GitHub Actions entry and reusable workflows

.azdo/pipelines/             Azure DevOps pipelines, templates, and variables
Docs/                        Setup, API, deployment, MCP, and operational documentation
reports/                     Planning and historical phase reports
Database/                    Legacy/manual SQL scripts and source data
azure.yaml                   Azure Developer CLI service configuration
```

Generated `bin/`, `obj/`, and IDE metadata folders are not source locations.

## 5. Languages and Frameworks

| Area | Language / framework | Evidence |
| --- | --- | --- |
| Web | C#, ASP.NET Core, Blazor Web App, .NET 10 | `src/TravelTracker/TravelTracker.csproj`, `Program.cs` |
| UI | Razor, HTML, CSS, QuickGrid | web components and package references |
| Data | C#, EF Core 10, SQL Server | `src/TravelTracker.Data/TravelTracker.Data.csproj` |
| Services and AI | C#, Azure AI Projects/OpenAI, Microsoft Agent packages | `src/TravelTracker.Services/TravelTracker.Services.csproj` |
| MCP | C#, .NET 10, Model Context Protocol SDK | projects under `src/TravelTracker.MCP/` |
| Database | T-SQL, SQL Server Database Project, DACPAC | `src/sql.database/` |
| Infrastructure | Azure Bicep | `infra/Bicep/` |
| Automation | YAML and PowerShell | `.github/workflows/`, `.azdo/pipelines/`, infrastructure scripts |
| Tests | xUnit, Moq, coverlet | `src/TravelTracker.Tests/TravelTracker.Tests.csproj` |

Read current project files for exact package versions.

## 6. Configuration and Secrets

`src/TravelTracker/appsettings.json` defines these primary sections:

- `SqlServer:ConnectionString`
- `AzureAd`
- `AzureMaps`
- `AzureAIFoundry`
- `ApiKey`, `ApiKey_UserID`, and `ApiKey_EmailAddress`
- standard ASP.NET Core logging and host settings

The checked-in values are local defaults or placeholders. Store real credentials in User Secrets, environment variables, GitHub/Azure DevOps secret stores, or Azure Key Vault. `DefaultAzureCredential` is used for Azure identity, with `VisualStudioTenantId` supported as a local tenant override.

Infrastructure parameters under `infra/Bicep/` cover App Service, Azure SQL, Key Vault, storage, monitoring, SignalR, managed identity, Azure Maps, Entra ID, and AI settings. The current composition deploys App Service for `deploymentType` values `webapp` and `all`; comments mentioning container or function deployment are not backed by active modules in this repository. Phase 6 adds one-worker App Service configuration, `Confirm`-only Travel Assistant settings, and an optional cross-subscription Foundry resource-group role-assignment module for a user-assigned identity. `.github/CreateGitHubSecrets.md` documents every GitHub secret/variable token consumed by `infra/Bicep/main.bicepparam` (including the Phase 6 Travel Assistant, Foundry RBAC, OpenAI, and Azure Maps tokens) and is kept in sync whenever new tokens are added.

## 7. Testing

The .NET test project is `src/TravelTracker.Tests/TravelTracker.Tests.csproj`. It uses xUnit, Moq, coverlet, Microsoft.NET.Test.Sdk, and EF Core InMemory, and contains controller, data, and service tests. Coverage-focused tests include destination and health controllers, destination service matching, configuration fallback, authentication, build-info loading, disabled assistant behavior, principal accessors, Copilot health checks, JSON/CSV data import, and in-memory repository queries/CRUD. On 2026-09-04, 240 tests passed with 60.03% line coverage and 49.49% branch coverage using `coverage.runsettings`; the next improvement targets are the remaining zero-coverage controllers/services and repository branches. On 2026-09-05, nullable flow warnings were removed from `ConfigController`, `Utilities`, `ClaimsTransformation`, and `Admin.razor`, along with duplicate using warnings in `Program.cs` and `globalUsings.cs`; the web project now builds with 0 warnings and 0 errors. The latest 244-test run reports `TravelTracker.Data` at 66.6% line and 27.6% branch coverage, with 65.2% line and 50.5% branch coverage overall.

Useful commands:

```powershell
dotnet test .\src\TravelTracker.Tests\TravelTracker.Tests.csproj
dotnet test .\src\TravelTracker.sln
dotnet build .\src\TravelTracker\TravelTracker.csproj
```

Some workflows still reference a `playwright/` directory, but that directory and root Playwright configuration are not present as of this review. Treat browser-test workflow steps as drift until the tests are restored or the workflows are revised.

As of 2026-09-04, the focused web build command succeeds with warnings, and the focused test command succeeds after the test project package references were normalized.

## 8. GitHub Actions

The current entry workflows are:

- `1-deploy-bicep.yml`: deploy Bicep infrastructure.
- `2.1-bicep-build-deploy-webapp.yml`: deploy infrastructure, build, and deploy the App Service web app.
- `4-build-deploy-dacpac.yml`: build and deploy the SQL DACPAC.
- `5-run-sql-script.yml`: execute a SQL script.
- `6-pr-scan-build.yml`: pull-request scan, build, and test.
- `7-scan-code.yml`: code and dependency scanning, with optional SBOM generation.
- `8-smoke-test-webapp.yml`: deployed web smoke-test entry point.
- `azure-dev.yml`: Azure Developer CLI workflow.

Reusable `template-*.yml` workflows implement configuration loading, Bicep, web build/deploy, DACPAC, SQL, scanning, SBOM, and smoke testing. Read an entry workflow and every called template together before changing contracts, permissions, secrets, or outputs.

The web build template publishes with a build-enabled `dotnet publish --no-restore` so the GitHub Copilot SDK's platform-specific native runtime is copied into the artifact. It verifies the executable Linux runtime path before uploading the artifact; do not change this to `--no-build` without preserving that runtime copy behavior.

## 9. Azure DevOps Pipelines

`.azdo/pipelines/` contains App Service infrastructure/build/deploy, PR, scan, smoke-test, and automated-test entry pipelines. Shared implementation lives under `pipes/`; environment and source settings live under `vars/`.

The Azure DevOps readme contains copied setup values and should be verified against current variable templates before use. Apply `.github/instructions/azure-devops-pipeline-instructions.md` when changing pipeline YAML.

## 10. SQL and Data Operations

`src/sql.database/sql.database.sqlproj` is the deployment schema authority. Objects under `src/sql.database/Travel/` define the `Travel` schema. Use its pre/post-deployment scripts and DACPAC workflows for deployed schema changes.

`src/sql.database/sql.database.sqlproj` is pinned to `Microsoft.Build.Sql` 2.0.0 for current Visual Studio SSDT task compatibility. `src/sql.database/Directory.Build.props` sets runtime-specific intermediate output (`obj/$(MSBuildRuntimeType)/`) so command-line and Visual Studio/MSBuild builds do not overwrite each other's restore assets.

The root `Database/` directory contains older manual scripts, comparison files, and source data. Do not assume those files supersede the SQL project.

For a schema change:

1. Update the appropriate object under `src/sql.database/`.
2. Review deployment and seed scripts.
3. Build the SQL project and validate the DACPAC workflow.
4. Update documentation and this map when ownership or deployment behavior changes.

## 11. Copilot Customization

`.github/copilot-instructions.md` is the repository-wide baseline. Focused instructions under `.github/instructions/` cover C#, Blazor/CSS, .NET structure, Bicep, GitHub Actions, Azure DevOps, SQL, testing, and general practices.

`.github/skills/`, `.github/agents/`, and `.github/prompts/` contain local reusable workflows and guidance. Use the directory contents as the source of truth rather than assuming a customization copied from another repository exists here.

## 12. Local Development and Validation

Prerequisites are the .NET 10 SDK and access to SQL Server. Azure services are needed for the associated authentication, maps, AI, and deployment features.

```powershell
dotnet restore .\src\TravelTracker.sln
dotnet build .\src\TravelTracker.sln
dotnet test .\src\TravelTracker.Tests\TravelTracker.Tests.csproj
dotnet run --project .\src\TravelTracker\TravelTracker.csproj
```

The app expects a usable SQL connection string for its repository and service registrations. Configure optional Entra ID, Azure Maps, Azure AI Foundry, and API-key settings through secure configuration providers.

## 13. Branching and Change Policy

The primary branch is `main`. Do not commit or push unless explicitly requested. Never commit directly to `main` or `master`; create a `feature/`, `fix/`, or `chore/` branch before a requested commit and open a pull request targeting `main`.

For source, infrastructure, workflow, test, or Copilot-customization changes:

1. Update focused documentation when behavior or workflow changes.
2. Update this map when locations, ownership, commands, configuration, or architecture change.
3. Run the narrowest relevant build, test, lint, or validation command.

## 14. Known Caveats and Drift Signals

- `azure.yaml` still contains a copied project name and points its web service at nonexistent `src/web/Website/`; use `src/TravelTracker/` when correcting azd configuration.
- The workspace build, publish, and watch tasks also point at a nonexistent web project rather than `src/TravelTracker/TravelTracker.csproj`.
- `src/TravelTracker/TravelTracker.csproj` contains duplicate package references, and `Program.cs` contains duplicate using directives; the focused web build succeeds but reports 21 warnings as of 2026-09-04.
- `src/TravelTracker.Tests/TravelTracker.Tests.csproj` has valid XML and its focused `dotnet test` command passes after duplicate and nested package references were removed.
- No `playwright/`, `package.json`, root Playwright configuration, `Dockerfile`, `PRODUCT.md`, `DESIGN.md`, or `CONTRIBUTING.md` exists in the current tree, despite references in some copied documentation and automation.
- `infra/Bicep/main.bicep` accepts deployment-type labels beyond App Service, but active deployment composition currently creates the web app only for `webapp` and `all`.
- Entra ID is optional at startup, while SQL configuration is effectively required for the registered application services.
- Historical reports describe planned phases and may lag current code. Verify claims against source before updating status.

## 14.1. Copilot SDK 1.0.11 Integration Status

**Phase 2: Durable Action Boundary (Completed)**

- Provider-neutral assistant reads and writes are owned by `ITravelAssistantActionService` and `ITravelAssistantActionConfirmationService`. Every public operation requires a trusted `TravelAssistantUserContext`; model-visible contracts cannot select a user.
- Place lookup now returns ranked `Found`, `Ambiguous`, or `NotFound` candidates with provider evidence, broader-query fallback, coordinate-divergence detection, cancellation, a one-second public-provider rate limit, bounded caching, and opaque 15-minute candidate IDs.
- Assistant location search is limited to 25 compact records and excludes comments/tags. Location-type resolution is case-insensitive and reports ambiguity instead of guessing. Duplicate checks use a targeted user/name/date/city/state query.
- Pending create-location commands are stored in `Travel.AssistantActions` as versioned canonical JSON protected with ASP.NET Core Data Protection, a SHA-256 payload hash, canonical idempotency key, rowversion, sanitized summary, expiry, and retention metadata. The key ring persists under `TravelAssistant:DataProtectionKeysPath` or the machine-local Travel Tracker application-data directory.
- Confirmation and cancellation use a serializable SQL transaction. Confirmation locks and validates the pending row, rechecks duplicates, inserts a location linked through unique nullable `Location.AssistantActionId`, and records the nonzero location ID. Retries return the prior result; rollback clears tracked state so failed writes remain pending.
- Location creation now propagates failures and rejects a zero persisted ID. The Locations page preserves user-facing failure handling. A hosted cleanup service expires 24-hour pending commands, clears terminal ciphertext, and removes sanitized audit rows after 90 days.
- Phase 2 coverage lives under `src/TravelTracker.Tests/Services/`; the EF mapping and DACPAC include the action ledger and idempotency constraints.

**Phase 3: Session Coordination & Non-Streaming Chat (Completed)**

- `GitHub.Copilot.SDK` is pinned directly to `1.0.11` in the web project. The services project retains a compile-only direct reference; excluding its runtime and build assets prevents duplicate CLI files during publish while the web project remains the single runtime-asset source.
- `CopilotRuntimeAccessor` is the singleton SDK owner. It configures `CopilotClientMode.Empty`, a writable home directory, disabled content capture, and a Foundry OpenAI provider using `/openai/v1/`, the Responses API, the configured deployment, and a `DefaultAzureCredential` bearer-token callback. Phase 6 infrastructure keeps the first release in `Confirm` mode and configures one App Service worker because SDK runtime state is instance-local.
- `CopilotRuntimeHostedService` performs abandoned-session cleanup before starting and pinging the real SDK runtime. It starts only when `CopilotSDK` is selected and assistant prerequisites are ready, and it uses graceful then forced shutdown.
- `CopilotSessionCoordinator` owns a global thread namespace, deterministic non-identifying SDK session IDs, immutable user ownership, per-user and instance quotas, idle eviction, and atomic activity/turn tracking. It rejects unknown, stale, and cross-user thread requests; the provider-neutral chat service converts only an authenticated user's stale/unknown thread into a newly generated thread and reports `thread_replaced`.
- Per-session `SemaphoreSlim` leases serialize turns. Queue waiting and the configured execution timeout are separate, and explicit deletion waits for an active turn before disposing the session and requesting SDK deletion.
- Session configuration is non-streaming and disables infinite sessions, memory, the session store, configuration and instruction discovery, file hooks, host Git operations, skills, and embedding retrieval. Token and embedding caches are in memory, and `AvailableTools` contains only source-qualified custom travel tools.
- `CopilotTravelToolFactory` creates a fresh asynchronous DI scope per invocation and binds trusted coordinator-owned user/thread context. The allowlist contains exactly `search_user_locations`, `get_location_types`, `lookup_place`, and `prepare_add_visited_location`; confirmation remains outside model control.
- `CopilotChatbotService` implements the provider-neutral `IChatbotService`, sends through `SendAndWaitAsync`, supplies server-authoritative time/timezone context, and maps cancellation, stale-session, and runtime failures to stable responses without exposing exception text.
- Startup cleanup is limited to `COPILOT_HOME/session-state`; size trimming preserves unrelated runtime-owned files. `TravelAssistant:MaxCopilotHomeBytes` defaults to 100 MB.
- Focused Phase 3 tests cover session reuse and isolation, quotas, serialization, timeout behavior, hardening, eviction/deletion, cleanup scope, real provider flow through the SDK boundary, time context, and sanitized failures. Release publish verifies a single `GitHub.Copilot.SDK.dll` and platform-native Copilot CLI. A live Foundry smoke requires deployment-specific credentials and configuration.

**Phase 4: Restricted Travel Tools (Completed)**

- `CopilotTravelToolNames` is the canonical source for the exact four-tool inventory used by the SDK allowlist, factory, permission handler, hooks, and tests. No shell, filesystem, web-fetch, process, code-editing, or arbitrary host tool is exposed.
- The three read tools skip SDK permission prompts. `prepare_add_visited_location` requires the custom deny-by-default permission handler, which returns an approve-once decision only for that preparation tool. It can create a pending action but cannot confirm or commit it.
- Model-visible schemas exclude user identity, credentials, connection strings, authorization decisions, and canonical commands. The preparation schema validates ratings from 0 through 5; application services remain authoritative for all field, candidate, date, location-type, and ownership validation.
- `get_location_types` returns at most 100 ordered name/description records. User location search remains capped at 25 compact records, and place lookup returns bounded opaque candidates.
- Each invocation resolves and asynchronously disposes a fresh DI scope. Tool closures capture only immutable coordinator-owned `TravelAssistantUserContext` and thread ID, preventing model-selected or cross-user identity.
- Pre-, post-, and failure hooks record only canonical tool name, correlation ID, elapsed duration, controlled result class, and a validated opaque action ID. Raw prompts, tool arguments/results, errors, tokens, comments, addresses, working directories, encrypted payloads, and reasoning are not logged.
- Phase 4 tests under `src/TravelTracker.Tests/Services/` verify the exact inventory and safe schemas, permission metadata and unknown-request denial, immutable context, fresh scopes, injection text as inert data, redacted telemetry, validated action IDs, and bounded ordered location types.

**Phase 5: Confirmation-Only Chat (Completed)**

- `ChatbotController` preserves `POST /api/chatbot/message` and adds `GET /api/chatbot/pending-actions`, `POST /api/chatbot/actions/{actionId}/confirm`, and `POST /api/chatbot/actions/{actionId}/cancel`. The controller derives the trusted user from the authenticated principal, accepts only an opaque action ID for writes, validates antiforgery on all POST routes, and maps stable action failures to `403`, `404`, `409`, `410`, or `503`; message failures also preserve `401` and `429`.
- `ITravelAssistantActionConfirmationService` exposes action-ID-only confirmation and cancellation for UI/API callers while retaining thread-bound overloads for internal validation. The implementation always verifies durable action ownership and derives the thread from the stored action when the caller does not supply an expected thread.
- `CopilotChatbotService` processes a stale or unknown authenticated thread once on a new generated thread and returns `thread_replaced`. Successful turns query the durable ledger for the newest pending action in the effective thread and expose its sanitized confirmation summary. The non-streaming SDK handle currently returns response text only; the result/UI contracts can render provider-supplied tool statuses without capturing raw tool payloads.
- `Chat.razor` resolves the authenticated user from the cascading authentication state, recovers all unexpired pending actions during initialization, renders assistant tool statuses and sanitized pending location/source summaries, and provides keyboard-operable confirmation controls. Buttons disable during execution and after terminal results; retryable persistence failures remain actionable. Successful confirmation links to `/locations`.
- Chat styling is scoped in `Chat.razor.css`, uses existing light/dark theme variables, includes visible keyboard focus, and adapts message and action layouts for narrow viewports.
- Phase 5 tests cover authorized principal binding, action-ID-only endpoint contracts and antiforgery metadata, stable status mappings, stale-thread replacement, pending-action propagation, recovery filtering, cancellation, transaction/idempotency behavior, and a deterministic Buffalo House prepare/recover/confirm flow resolving `RV Park` on `2026-08-31`. Repeated confirmation returns the same nonzero location ID and creates exactly one location.

**Phase 6: Location Summary Context and Usage Diagnostics (Completed)**

- `ILocationSummaryRepository`/`LocationSummaryRepository` (`src/TravelTracker.Data/Repositories/`) execute `[Travel].[usp_LocationSummary]` via raw ADO.NET on the shared `TravelTrackerDbContext` connection, keyed by username or email. All 7 result sets are read fresh on every turn (no caching) and flattened into `## SectionName` / `column=value` text blocks for prompt injection.
- `CopilotChatbotService.GetChatResponseAsync` resolves the calling user through `IUserService.GetUserByIdAsync` (falling back to empty identity fields only if the user record is missing) and passes real `EntraIdUserId`/`Username`/`Email` into `TravelAssistantUserContext`. `BuildTurnPromptAsync` appends the location summary text (or a graceful "no data available" placeholder on lookup failure) into the server-authoritative context section of every prompt.
- `ICopilotSessionHandle.SendAndWaitAsync` now returns `CopilotTurnResponse`, aggregating model call count and summed input/output/cache-read/cache-write tokens and AI Credits `Cost` from every `AssistantUsageEvent` the SDK raises during the turn (subscribed via `CopilotSession.On<AssistantUsageEvent>`). `CopilotSessionHandle` wraps the experimental `AssistantUsageData.Cost` API with a scoped `#pragma warning disable GHCP001`.
- `ChatTurnResult`/`ChatUsageInfo` carry a `Usage` payload (wall-clock `DurationSeconds` from a `Stopwatch` around the SDK call, `TurnCount` from the existing session tracking, model call count, token sums, and total cost) through to `ChatbotController.ToChatResponse`, which maps it to the new `ChatUsageDto` on `ChatResponse`.
- `Chat.razor` renders a `chat-usage-info` diagnostics line under each assistant reply (duration, turn count, input/output tokens, cache tokens when present, and AI Credits cost), styled in `Chat.razor.css` alongside the existing tool-status list.

## 15. High-Value References

- [README.md](README.md): product overview, architecture, and basic setup; links to the separate development-status record.
- [reports/Report-Status.md](reports/Report-Status.md): concise current development-status record.
- [Docs/API-Documentation.md](Docs/API-Documentation.md): REST and MCP API reference.
- [Docs/MCP-SETUP.md](Docs/MCP-SETUP.md): MCP configuration and usage.
- [Docs/CHATBOT_SETUP.md](Docs/CHATBOT_SETUP.md): AI chatbot configuration.
- [Docs/DatabaseSetup.md](Docs/DatabaseSetup.md): database setup guidance.
- [Docs/Infra_As_Code.md](Docs/Infra_As_Code.md): infrastructure overview.
- [reports/Travel-Tracker-Application-Plan.md](reports/Travel-Tracker-Application-Plan.md): original product plan and requirements.
- [.github/copilot-instructions.md](.github/copilot-instructions.md): repository-wide agent policy.
