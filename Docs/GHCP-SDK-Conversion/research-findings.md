# GitHub Copilot SDK Conversion Research

## Executive Finding

The conversion is feasible and should use the GitHub Copilot SDK as the orchestration runtime, not merely as another text-completion client. The SDK can call typed C# tools, maintain conversational sessions, emit tool and streaming events, use an Azure AI Foundry OpenAI-compatible endpoint, and acquire Foundry bearer tokens through `DefaultAzureCredential`.

The repository already contains the domain operations needed for the example request, but they are not assembled into an agent-safe workflow:

- `LocationLookupAPIService` resolves a named place through Nominatim and Photon.
- `ILocationTypeService` validates `RV Park` and other configured types.
- `ILocationService.CreateLocationAsync` validates and persists a location.
- `LocationTools` exposes read-only MCP operations.
- `Chat.razor` provides an authenticated chat UI.

The missing layer is a restricted, authenticated application tool boundary that binds the current user on the server, validates and deduplicates proposed writes, records action state, and gives the UI a reliable confirmation contract.

## Sources Reviewed

### Golden repositories

- `lluppesms/dadabase.demo`
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
| Persistence | SDK sessions can persist state, but storage and scale-out ownership must be deliberate. | Disable SDK memory/session-store features in the first release and persist only application action records. |
| Telemetry | SDK telemetry can use OpenTelemetry and can optionally capture content. | Export timing and tool metadata, with content capture disabled by default. |

## Golden-Code Comparison

The `dadabase.demo` conversion established a useful baseline:

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

Current APIs accept `userId` in routes or query strings and then validate it. Agent tools should not include `userId` in their schemas. The authenticated server context must bind user identity before the tool handler executes.

### Duplicate and retry handling

There is no durable agent action ID. A model retry, browser retry, or runtime reconnect could insert the same visit twice. The write workflow needs an idempotency key, a pending action record, duplicate detection, and an exactly-once commit path.

### Relative dates

The example contains `Yesterday`. The model must receive an authoritative current local date and timezone. The application must validate the resulting ISO date using `TimeProvider`; it must not rely on the model's unstated clock.

## Recommended Tool Boundary

The Copilot runtime should expose these initial custom tools:

| Tool | Kind | Inputs visible to model | Server-bound values | Result |
|---|---|---|---|---|
| `search_user_locations` | Read | query, optional state/date range | user ID | Compact matching locations |
| `get_location_types` | Read | none | none | Valid type names and descriptions |
| `lookup_place` | Read | name, city, state, optional address/ZIP | none | Normalized address and coordinates with source/confidence |
| `prepare_add_visited_location` | Prepare | place fields, type, ISO start/end dates, optional notes | user ID, thread ID, action ID, current time | Existing match, validation error, pending action, or auto-execution result |

No generic SQL, shell, filesystem, arbitrary URL, or model-supplied-user tool should be exposed. The database commit should occur in application code through `ConfirmActionAsync`, or inside the prepare tool only when the explicitly configured personal-deployment policy permits automatic execution.

## Example Request Walkthrough

For a prompt received on 2026-09-01 in `America/Chicago`:

> Yesterday I stayed at the Buffalo House RV Park in Duluth MN. Can you add that entry to my list of locations visited?

The expected orchestration is:

1. The session prompt supplies current local date `2026-09-01`, timezone `America/Chicago`, and the instruction to convert relative dates to ISO dates.
2. Copilot calls `get_location_types` and identifies `RV Park` as a valid exact type.
3. Copilot calls `lookup_place` with the name, city `Duluth`, and state `MN`.
4. The application returns normalized address, ZIP, latitude, and longitude from the public lookup service.
5. Copilot calls `prepare_add_visited_location` with start date `2026-08-31` and normalized fields. It cannot supply a user ID.
6. The application checks ownership context, required fields, duplicate visits, and location type; then records a pending action with an opaque ID and expiration.
7. In confirmation mode, the chat UI displays the proposed location and Confirm/Cancel controls. Confirm executes the action exactly once through `ILocationService` and records the created location ID.
8. In explicitly enabled automatic mode, the same action service commits immediately and returns the created location ID.
9. The assistant reports success only when the application returns a persisted nonzero location ID.

## Architectural Decision

Use in-process Copilot custom tools backed by an SDK-independent application action service. Do not route the web app's own agent through its HTTP MCP server in the first release.

This choice provides the shortest authorization path, avoids forwarding the shared API key, avoids accepting a model-provided user ID, reuses scoped services, and keeps business validation testable without the Copilot runtime. The existing MCP tools can later delegate to the same action service so external agents receive equivalent behavior without duplicating business rules.

## Research Caveats

- The root SDK README contains older wording that says BYOK is key-only, while the current .NET provider API, managed-identity documentation, and both golden samples demonstrate bearer-token providers. The implementation must compile and run the exact `1.0.11` API before infrastructure cleanup.
- The golden repository validates Foundry completion and provider selection, not this repository's custom tools, confirmation UI, concurrent sessions, or idempotent writes.
- Public geocoders can return an incorrect first match. A low-confidence or ambiguous result must be confirmed and never auto-executed.
- A single in-process runtime is suitable for the first deployment. Scale-out requires sticky routing or a separately hosted Copilot runtime and distributed session coordination.
