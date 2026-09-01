# GitHub Copilot SDK Conversion Research

## Executive Finding

The conversion is feasible and should use the GitHub Copilot SDK as the orchestration runtime, not merely as another text-completion client. The SDK can call typed C# tools, maintain conversational sessions, emit tool and streaming events, use an Azure AI Foundry OpenAI-compatible endpoint, and acquire Foundry bearer tokens through `DefaultAzureCredential`.

The repository already contains the domain operations needed for the example request, but they are not assembled into an agent-safe workflow:

- `LocationLookupAPIService` resolves a named place through Nominatim and Photon.
- `ILocationTypeService` validates `RV Park` and other configured types.
- `ILocationService.CreateLocationAsync` validates and persists a location.
- `LocationTools` exposes read-only MCP operations.
- `Chat.razor` provides an authenticated chat UI.

The missing layer is a restricted, authenticated application tool boundary that binds the current user on the server, validates and deduplicates proposed writes, stores an immutable executable command, records action state, and gives the UI a reliable confirmation contract.

## Sources Reviewed

### Reference repositories

- Earlier Copilot SDK sample
  - `src/web/Website/Services/CopilotSdkChatService.cs`
  - `src/web/Website/Services/AgentFrameworkChatService.cs`
  - `src/web/Website/Helpers/AiServiceCollectionExtensions.cs`
  - `Docs/Updates/Refactor-AI-Calls/*`
- `lluppesms/simple.ghcp.sdk.byok`
  - `src/web/Services/GHCP_SDK_Service.cs`
  - managed-identity and App Service configuration in `infra/Bicep`

### Current SDK documentation

- GitHub Copilot SDK repository and .NET README
- Context7 library `/github/copilot-sdk`
- NuGet flat-container index for `GitHub.Copilot.SDK`

### Travel Tracker code

- Current chat service, interface, controller, UI, and tests
- Location lookup, location type, persistence services, and models
- MCP hosts and read-only location tools
- Web registration, settings, and Bicep deployment configuration

## Verified SDK Facts

| Topic | Verified finding | Design implication |
|---|---|---|
| Version | Latest stable NuGet version reviewed on 2026-09-01 is `1.0.11`; later `1.0.13` builds are preview only. | Pin `GitHub.Copilot.SDK` to `1.0.11`. |
| Status | The SDK is generally available and follows semantic versioning. | Treat it as production-capable, while canarying because the app behavior is new. |
| Runtime | The .NET package bundles `copilot-runtime` and `runtime.node`; the client communicates over JSON-RPC. | Validate publish output and App Service process startup, not only compilation. |
| Foundry auth | `ProviderConfig` supports an OpenAI-compatible base URL and bearer-token callback. The golden sample uses `DefaultAzureCredential` with the Cognitive Services scope. | Use managed identity and do not store a model API key for the Copilot provider. |
| Tools | `CopilotTool.DefineTool` exposes typed C# handlers with generated JSON schemas. | Wrap application services directly instead of giving the model generic HTTP access. |
| Tool controls | Sessions support explicit tool lists, per-tool permission behavior, hooks, and permission handlers. | Run in empty mode and allow only Travel Tracker tools. |
| Sessions | The client supports multiple sessions, session IDs, events, streaming, and disposal. | Share one runtime client, isolate sessions by authenticated user and thread, and serialize turns per session. |
| Persistence | Disposing an SDK session preserves resumable data; permanent cleanup requires `DeleteSessionAsync`. | Disable memory/session-store features, delete ephemeral sessions on eviction, and persist only application action records. |
| Telemetry | SDK telemetry can use OpenTelemetry and can optionally capture content. | Export timing and tool metadata, with content capture disabled by default. |

## Golden-Code Comparison

The earlier sample conversion established a useful baseline:

1. Keep a small provider abstraction.
2. Retain Agent Framework as a fallback.
3. Select the provider through configuration.
4. Configure the Copilot SDK session with a Foundry URL, model name, token scope, and managed-identity bearer-token provider.
5. Dispose the client and session after the call.

Travel Tracker must go further than the golden implementation because its goal includes state-changing tools and multi-turn chat. Creating and disposing a client for every message is acceptable for a completion-only sample, but it would repeatedly start the bundled runtime and lose efficient session ownership. Travel Tracker should instead use one hosted `CopilotClient` per app instance and user-isolated sessions managed by a coordinator.

## Current Travel Tracker Gaps

### Completion instead of agency

`ChatbotService` gathers up to 250 locations plus destinations and injects them into a prompt. It calls `AIAgent.RunAsync`, but no functions are registered. The model can describe data; it cannot perform a location lookup or write a location.

### Synthetic thread state

The service returns a generated thread ID, but that ID is not connected to an AI conversation. The service stores one previous context payload and user ID in mutable fields. This does not provide durable or concurrent multi-turn session semantics.

### Missing write surface

`LocationTools` contains only GET-style operations. The create endpoint in `LocationsController` is commented out. The Locations page writes by calling `ILocationService` directly.

### Unsafe failure shape for automation

`LocationService.CreateLocationAsync` catches every exception and returns an empty `Location`. An agent tool could mistake that value for success unless the write boundary converts persistence outcomes into explicit success and error contracts.

### Identity and authorization boundary

Current APIs accept `userId` in routes or query strings and then validate it. The global API key can authorize any requested user, and `IHttpContextAccessor` is not a reliable identity source throughout a long-lived Blazor circuit. The assistant surface must require authentication, derive a principal from `HttpContext.User` for controllers or `AuthenticationStateProvider` for Blazor, map that principal to one internal user, and reject the global-key impersonation path. Agent tools must not include `userId` in their schemas.

### Duplicate and retry handling

There is no durable agent action ID. A model retry, browser retry, or runtime reconnect could insert the same visit twice. The write workflow needs a canonical idempotency key, an immutable versioned command payload, unique database constraints, and one transaction that claims the action, checks duplicates, inserts the location, and records completion.

### Relative dates

The example contains `Yesterday`. The model must pass the original date expression to a deterministic application resolver backed by `TimeProvider`, `TimeZoneInfo`, and `DateOnly`. A model-proposed ISO date is advisory and must match the server result.

### Place ambiguity

The current public lookup requests one Nominatim result and optionally replaces its coordinates with Photon’s first result. It cannot distinguish a confident match from an ambiguous or conflicting match, and an exact Buffalo House query may return no result. The agent workflow needs multiple candidates, provider evidence, broader-query fallback, scoring, and explicit user selection when confidence is insufficient.

## Recommended Tool Boundary

The Copilot runtime should expose these initial custom tools:

| Tool | Kind | Inputs visible to model | Server-bound values | Result |
|---|---|---|---|---|
| `search_user_locations` | Read | query, optional state/date range | user ID | Compact matching locations |
| `get_location_types` | Read | none | none | Valid type names and descriptions |
| `lookup_place` | Read | name, city, state, optional address/ZIP | none | Ranked candidates with opaque candidate IDs, provider evidence, and confidence |
| `prepare_add_visited_location` | Prepare | selected candidate ID, place fields, type, original date expression, optional proposed ISO dates/notes | user ID, thread ID, action ID, current time | Existing match, validation error, or durable pending action |

No generic SQL, shell, filesystem, arbitrary URL, or model-supplied-user tool should be exposed. The first release commits only through provider-neutral application code after authenticated confirmation. SDK permission approval permits the prepare call; it never authorizes the later database commit.

## Example Request Walkthrough

For a prompt received on 2026-09-01 in `America/Chicago`:

> Yesterday I stayed at the Buffalo House RV Park in Duluth MN. Can you add that entry to my list of locations visited?

The expected orchestration is:

1. The session prompt supplies current local date `2026-09-01`, timezone `America/Chicago`, and instructs the model to preserve the original relative-date expression for server resolution.
2. Copilot calls `get_location_types` and identifies `RV Park` as a valid exact type.
3. Copilot calls `lookup_place` with the name, city `Duluth`, and state `MN`. The resolver queries multiple candidates through configured providers, retries a broader query when necessary, and scores name/city/state agreement.
4. The application returns one confident candidate or asks the user to choose among compact candidates. No write is prepared for an unresolved result.
5. Copilot calls `prepare_add_visited_location` with the selected candidate, original expression `Yesterday`, proposed date `2026-08-31`, and normalized fields. It cannot supply a user ID.
6. The application independently resolves `Yesterday` to `2026-08-31`, verifies the proposal, checks ownership, required fields, duplicates, and location type, then stores a versioned canonical command with an opaque action ID and expiration.
7. The chat UI displays the proposed location and Confirm/Cancel controls. Confirmation loads the stored command, verifies its hash and ownership, and performs the action claim, duplicate check, location insert, and completion update in one SQL transaction.
8. The assistant reports success only when the application returns a persisted nonzero location ID. Retries return the original result.

## Architectural Decision

Use in-process Copilot custom tools backed by an SDK-independent application action service and a provider-neutral confirmation service. Persist a versioned canonical command in the action ledger; a hash alone is not executable. Do not route the web app's own agent through its HTTP MCP server in the first release.

This choice provides the shortest authorization path, avoids forwarding the shared API key, avoids accepting a model-provided user ID, reuses scoped services, and keeps business validation testable without the Copilot runtime. The existing MCP tools can later delegate to the same action service so external agents receive equivalent behavior without duplicating business rules.

## Research Caveats

- The root SDK README contains older wording that says BYOK is key-only, while the current .NET provider API, managed-identity documentation, and both golden samples demonstrate bearer-token providers. The implementation must compile and run the exact `1.0.11` API before infrastructure cleanup.
- The golden repository validates Foundry completion and provider selection, not this repository's custom tools, confirmation UI, concurrent sessions, or idempotent writes.
- Public geocoders can return an incorrect first match. A low-confidence or ambiguous result must be confirmed and never auto-executed.
- A single in-process runtime is suitable for the first deployment. Scale-out requires sticky routing or a separately hosted Copilot runtime and distributed session coordination.
- `CopilotClientOptions.BaseDirectory` controls `COPILOT_HOME` state, not the published runtime-binary path. The deployment must validate both a writable ephemeral home and executable Linux runtime assets.
