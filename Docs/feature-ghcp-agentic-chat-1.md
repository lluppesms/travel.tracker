---
goal: Implement secure GitHub Copilot SDK chat actions for Travel Tracker
version: 1.0
date_created: 2026-09-01
last_updated: 2026-09-01
owner: Travel Tracker maintainers
status: 'Planned'
tags: [feature, architecture, ai, github-copilot-sdk, tool-calling]
---

# Introduction

![Status: Planned](https://img.shields.io/badge/status-Planned-blue)

This plan replaces the current Azure OpenAI chat execution path with a feature-flagged GitHub Copilot SDK provider and adds secure, user-confirmed application actions. The target scenario is: “Yesterday I stayed at the Buffalo House RV Park in Duluth MN. Can you add that entry to my list of locations visited?” The completed flow resolves the relative date in the user's timezone, looks up the park, selects the existing `RV Park` location type, checks for duplicates, presents a draft, requires explicit user confirmation, and creates exactly one location for the authenticated user.

## Research Summary

- The golden implementation was introduced in `lluppesms/dadabase.demo` by PR #85 and commit [`d2b498a`](https://github.com/lluppesms/dadabase.demo/commit/d2b498a150cae86a63a77536d43e3c17e85cac35). It retains `AgentFrameworkChatService`, adds `CopilotSdkChatService`, selects the provider through configuration, and uses GitHub Copilot SDK with an Azure AI Foundry BYOK provider.
- The golden `CopilotSdkChatService` uses `GitHub.Copilot.SDK` 1.0.8, `CopilotClient`, `CopilotSession`, `ProviderConfig`, `WireApi = "responses"`, and `DefaultAzureCredential`. It does not implement tools, creates a new runtime and session for every completion, and combines system and user prompts into one user message.
- The current official SDK release is [1.0.11](https://github.com/github/copilot-sdk/releases/tag/v1.0.11). The [.NET SDK documentation](https://github.com/github/copilot-sdk/blob/main/dotnet/README.md) confirms support for custom tools, `SystemMessage`, permission handlers, session resumption, event telemetry, MCP servers, and `CopilotClientMode.Empty` for capability-restricted multi-tenant hosts.
- Travel Tracker already has the required domain operations: `ILocationLookupService` resolves place details, `ILocationTypeService` validates types, and `ILocationService` creates and queries locations. Existing MCP location tools are read-only and use HTTP/API-key loopback; they are not the correct write path for the in-application chat.
- The current `ChatbotService` sends a large preloaded data snapshot on every new scoped instance, does not expose model-callable tools, and returns a synthetic thread identifier. `Chat.razor` already supplies the authenticated internal user ID and is the target interactive host.

## Architecture Decision

Use the GitHub Copilot SDK as the orchestration runtime with Azure AI Foundry as the BYOK model provider. Execute narrowly scoped custom tools against in-process application services. Do not expose the runtime's shell, filesystem, general network, or built-in web tools. Bind each session to the server-derived authenticated user ID, and never accept a user ID as a model-generated tool parameter.

The first release supports mutating actions only in the Blazor chat UI. `ChatbotController` remains read-only because its stateless REST contract cannot securely complete the in-memory human-confirmation flow across scaled instances. A future REST or MCP write surface requires an external session and pending-action store.

## 1. Requirements & Constraints

- **REQ-001**: Replace direct `AzureOpenAIClient.AsAIAgent` chat execution with a selectable GitHub Copilot SDK provider while retaining the existing provider as a rollback option during rollout.
- **REQ-002**: Resolve the sample Buffalo House request through model-directed calls to lookup, location-type, duplicate-check, and draft-creation tools.
- **REQ-003**: Require a user click in `Chat.razor` before any location is persisted.
- **REQ-004**: Return and reuse real conversation thread identifiers with bounded server-side session lifetime.
- **REQ-005**: Resolve relative dates using an IANA timezone supplied by the browser and an injected `TimeProvider`; use UTC only when no valid timezone is available and disclose that fallback in the draft.
- **REQ-006**: Populate both `Location.LocationType` and `Location.LocationTypeId` from the same validated `LocationType` record.
- **REQ-007**: Preserve existing read-only chat behavior and friendly error responses when the Copilot SDK provider is disabled or unavailable.
- **REQ-008**: Treat ambiguous or failed location lookups as clarification requests; do not create a pending action until required fields are resolved.
- **REQ-009**: Detect duplicate submissions and confirmation retries so one approved pending action creates at most one database row.
- **SEC-001**: Derive the user ID from the authenticated host context and capture only that immutable ID in tool delegates.
- **SEC-002**: Resolve scoped repositories and services through a new `IServiceScope` for every tool invocation; never capture a scoped service or `DbContext` in a long-lived Copilot session.
- **SEC-003**: Run the SDK in `CopilotClientMode.Empty` with an explicit per-instance writable base directory and an allowlist containing only Travel Tracker custom tools.
- **SEC-004**: Do not use `PermissionHandler.ApproveAll`. Reject all non-allowlisted permission requests and all unavailable-user or managed-approval requests.
- **SEC-005**: Do not expose a model-callable commit tool. The draft tool returns an opaque `pendingActionId`; only the host confirmation handler may execute the stored action.
- **SEC-006**: Bind pending actions to user ID, thread ID, canonical payload hash, creation time, expiration time, and single-use status.
- **SEC-007**: Do not log prompts, access tokens, API keys, raw authorization headers, or full tool payloads. Log operation names, correlation IDs, duration, outcome, and redacted validation failures.
- **SEC-008**: Apply input length limits, model-call timeout, tool-call timeout, per-user rate limits, and cancellation propagation.
- **CON-001**: The solution targets .NET 10 and must use a GitHub Copilot SDK release that explicitly supports that target.
- **CON-002**: Azure AI Foundry must support the `/openai/v1` Responses API for the configured deployment.
- **CON-003**: The Copilot runtime child process must execute in the target Linux Azure App Service sandbox.
- **CON-004**: Initial session and pending-action storage is per application instance; production must use instance affinity and a single instance until state is externalized.
- **CON-005**: Rollback to the current provider restores read-only conversational behavior, not action capability.
- **GUD-001**: Prefer in-process domain services over loopback HTTP calls and API keys.
- **GUD-002**: Keep the provider adapter, session lifecycle, tool definitions, action execution, and UI concerns independently testable.
- **PAT-001**: Use the golden provider-selection and Azure Foundry bearer-token pattern, corrected for client reuse, first-class system messages, restricted capabilities, and real session continuity.

## 2. Implementation Steps

### Implementation Phase 1

- GOAL-001: Complete a deployment go/no-go spike before changing production chat behavior.

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-001 | Add a temporary, non-production integration test or diagnostic endpoint that starts `GitHub.Copilot.SDK` 1.0.11 on the target Linux App Service using `CopilotClientMode.Empty` and a per-instance local temporary `BaseDirectory`; remove the diagnostic endpoint after the spike. | | |
| TASK-002 | Configure an Azure AI Foundry BYOK `ProviderConfig` with `Type = "openai"`, `<FoundryResourceUrl>/openai/v1`, `WireApi = "responses"`, and `DefaultAzureCredential` using `https://cognitiveservices.azure.com/.default`. | | |
| TASK-003 | Verify managed identity authorization, executable runtime startup, one non-streaming completion, cancellation, shutdown, and absence of writes to the shared application content directory. | | |
| TASK-004 | Record a go decision only when runtime startup and one live completion succeed in the deployed sandbox. Stop implementation if the runtime cannot execute or the configured model does not support the Responses API. | | |

### Implementation Phase 2

- GOAL-002: Introduce provider boundaries and strongly typed configuration without changing default behavior.

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-005 | Add `GitHub.Copilot.SDK` 1.0.11 to `src/TravelTracker.Services/TravelTracker.Services.csproj` after running the dependency advisory check; add `Microsoft.Extensions.AI` only if it is not supplied transitively for `CopilotTool.DefineTool`. | | |
| TASK-006 | Add `src/TravelTracker.Data/Configuration/CopilotSdkSettings.cs` with provider name, Foundry resource URL, model name, token scope, base-directory root, session TTL, pending-action TTL, maximum active sessions, request timeout, and tool timeout settings. | | |
| TASK-007 | Add `src/TravelTracker.Services/Interfaces/IAiChatProvider.cs` for provider-neutral session creation, message execution, and disposal contracts; keep application orchestration out of the provider adapter. | | |
| TASK-008 | Move the existing Agent Framework execution behind `AgentFrameworkChatProvider` and add `CopilotSdkChatProvider`; select the implementation through a validated `AI:Provider` setting with `AgentFramework` as the initial default. | | |
| TASK-009 | Update `src/TravelTracker/Program.cs` and `src/TravelTracker/appsettings.json` to bind and validate options, register `DefaultAzureCredential`, register provider services, and fail startup with a non-secret configuration error when the selected provider is incomplete. | | |

### Implementation Phase 3

- GOAL-003: Add safe Copilot runtime and conversation lifecycle management.

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-010 | Add `src/TravelTracker.Services/Services/CopilotRuntimeService.cs` as a singleton hosted service that owns one `CopilotClient`, starts it once, uses `CopilotClientMode.Empty`, and gracefully stops or force-stops it during application shutdown. | | |
| TASK-011 | Add `src/TravelTracker.Services/Services/ChatSessionManager.cs` with a cryptographically random thread ID, immutable user binding, per-session asynchronous lock, last-access timestamp, cancellation source, bounded capacity, TTL eviction, and deterministic session disposal. | | |
| TASK-012 | Configure each `CopilotSession` with `SystemMessage` in append mode, the Azure Foundry provider, only custom Travel Tracker tools in `AvailableTools`, disabled cross-session memory, disabled cross-session store, and a custom deny-by-default permission handler. | | |
| TASK-013 | Add a scheduled cleanup path that evicts expired sessions and pending actions, disposes SDK resources, and emits aggregate lifecycle metrics without message content. | | |
| TASK-014 | Reject a thread ID when it belongs to another authenticated user, is expired, or is unknown; create and return a new thread without disclosing whether another user's thread exists. | | |

### Implementation Phase 4

- GOAL-004: Add read and draft tools that cannot mutate persisted data.

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-015 | Add `src/TravelTracker.Services/Models/ChatToolContracts.cs` containing bounded, JSON-serializable request and response records for location lookup, location types, location search, duplicate results, draft actions, clarification status, and tool errors. | | |
| TASK-016 | Add `src/TravelTracker.Services/Services/TravelTrackerChatToolFactory.cs`; create tools with `CopilotTool.DefineTool`, capture only trusted user ID, thread ID, timezone ID, and `IServiceScopeFactory`, and open a new service scope for each invocation. | | |
| TASK-017 | Define `lookup_location` using `ILocationLookupService.LookupLocationAsync`; return structured candidates or a structured clarification requirement when name, city/state, or lookup confidence is insufficient. | | |
| TASK-018 | Define `list_location_types` using `ILocationTypeService.GetAllLocationTypesAsync`; return IDs and names so the model selects an existing value such as `RV Park`. | | |
| TASK-019 | Define `search_locations` using `ILocationService` to return a small user-owned result set for questions and duplicate detection; never accept a user ID from tool arguments. | | |
| TASK-020 | Define `prepare_location_entry`; require canonical name, validated location type, ISO visit date, resolved city/state, and coordinates; revalidate all fields server-side, check duplicates, and store a pending draft without calling `CreateLocationAsync`. | | |
| TASK-021 | Include the current local date and validated timezone in the session context so “yesterday” becomes an ISO date before `prepare_location_entry`; include the timezone or UTC fallback in the displayed draft. | | |

### Implementation Phase 5

- GOAL-005: Implement host-enforced confirmation and idempotent location creation.

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-022 | Add `src/TravelTracker.Services/Interfaces/IPendingChatActionStore.cs` and an in-memory bounded implementation that stores drafts by opaque random ID with user/thread/payload binding, expiration, status, and resulting location ID. | | |
| TASK-023 | Add `src/TravelTracker.Services/Services/ChatActionExecutor.cs`; expose host-only confirm and cancel methods, revalidate ownership and expiration, atomically claim an action, create a fresh service scope, and execute the canonical draft through `ILocationService.CreateLocationAsync`. | | |
| TASK-024 | Resolve the selected location type again at confirmation, set both `LocationType` and `LocationTypeId`, set authenticated `UserId`, preserve the canonical visit date, and reject changed or invalid drafts. | | |
| TASK-025 | Make confirmation single-use and retry-safe: concurrent or repeated confirmation returns the existing result and never inserts a second row; cancellation permanently prevents execution. | | |
| TASK-026 | Keep create, update, and delete operations absent from the model's tool allowlist. Defer MCP write tools and REST write actions until they can use the same externalized human-confirmation boundary. | | |

### Implementation Phase 6

- GOAL-006: Integrate real sessions, confirmations, and user feedback into the existing chat surfaces.

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-027 | Replace the tuple in `src/TravelTracker.Services/Interfaces/IChatbotService.cs` with typed request and response contracts that carry thread ID, message, timestamp, pending action summary, and action result; include cancellation and invocation channel. | | |
| TASK-028 | Refactor `src/TravelTracker.Services/Services/ChatbotService.cs` to delegate to the selected provider and session manager, remove preloaded 250-location context and the mutable previous-context cache, and rely on tools for current data. | | |
| TASK-029 | Update `src/TravelTracker/Components/Pages/Chat.razor` to capture the browser IANA timezone, send cancellation tokens, render pending location details, provide Confirm and Cancel buttons, disable duplicate submission, and show the created location result. | | |
| TASK-030 | Ensure the Confirm button calls the host-only `ChatActionExecutor` path directly. Do not send a confirmation token or instruction back through the model. | | |
| TASK-031 | Update `src/TravelTracker/Controllers/ChatbotController.cs` to use the typed contract in read-only mode, derive and validate the user through `IAuthenticationService`, reject interactive action confirmation, and document that REST mutation is unsupported in version 1. | | |
| TASK-032 | Update chat copy to advertise draft-and-confirm location creation and give the Buffalo House prompt as an example. Preserve accessible keyboard operation, focus handling, status announcements, and loading states. | | |

### Implementation Phase 7

- GOAL-007: Verify behavior, isolation, failure handling, and the target user journey.

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-033 | Add provider-option and DI tests covering valid selection, Agent Framework fallback, invalid Copilot configuration, runtime startup failure, cancellation, and clean shutdown. | | |
| TASK-034 | Add tool tests covering schema descriptions, input limits, user isolation, scope-per-call behavior, location lookup success/failure/ambiguity, valid and invalid location types, duplicate detection, and redacted errors. | | |
| TASK-035 | Add deterministic relative-date tests with a fake `TimeProvider` and browser timezone, including UTC-boundary cases and invalid-timezone fallback. | | |
| TASK-036 | Add confirmation tests covering expiry, cancellation, replay, concurrency, cross-user access, cross-thread access, payload mismatch, duplicate submission, and rejection when the model attempts to commit without a host confirmation. | | |
| TASK-037 | Update `src/TravelTracker.Tests/Controllers/ChatbotControllerTests.cs` for typed responses, authenticated ownership, read-only REST behavior, and unsupported confirmation attempts. | | |
| TASK-038 | Add component tests for draft rendering, accessible Confirm/Cancel controls, error recovery, duplicate-click prevention, and successful list refresh or navigation. | | |
| TASK-039 | Add an integration test with fixed time and timezone proving the exact Buffalo House prompt causes lookup, `RV Park` type resolution, duplicate check, pending draft, no pre-confirmation insert, host confirmation, and exactly one persisted row with matching type ID and expected visit date. | | |
| TASK-040 | Add an ambiguity test proving “add Buffalo House I visited yesterday” requests city/state, then completes lookup, draft, confirmation, and one insert after the user supplies “Duluth, MN.” | | |
| TASK-041 | Run existing solution build and tests, targeted Copilot SDK integration tests, secret scanning, code review, and CodeQL before rollout. | | |

### Implementation Phase 8

- GOAL-008: Deploy safely with observable rollback and explicit scale constraints.

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-042 | Update `Docs/CHATBOT_SETUP.md` with provider configuration, local and managed-identity authentication, Foundry role requirements, user-secret commands, Linux runtime requirements, writable-directory rules, timeouts, and troubleshooting. | | |
| TASK-043 | Update `Docs/MCP-SETUP.md` to state that MCP tools remain read-only and that application chat actions use in-process services plus host confirmation. | | |
| TASK-044 | Add metrics for runtime availability, active/evicted sessions, model latency, tool count/duration/failure, pending/confirmed/cancelled/expired actions, permission denials, rate-limit rejections, and duplicate prevention. | | |
| TASK-045 | Deploy with the Agent Framework provider still active, enable Copilot SDK for a controlled environment, execute the Buffalo House smoke test, and monitor errors and resource usage before widening exposure. | | |
| TASK-046 | Roll back by selecting `AgentFramework` without removing the new code; communicate that rollback preserves read-only chat but disables action tools. | | |
| TASK-047 | Before horizontal scale-out or REST/MCP writes, replace in-memory session and pending-action storage with a shared protected store, add distributed locking/idempotency, and validate session routing across instances. | | |

## 3. Alternatives

- **ALT-001**: Copy the golden `CopilotSdkChatService` unchanged. Rejected because it starts a runtime per call, has no conversation continuity, treats the system prompt as user content, and exposes no function tools.
- **ALT-002**: Let the model call the existing REST API and MCP server. Rejected for the first release because it adds loopback latency and API-key handling, existing MCP tools are read-only, and the HTTP MCP endpoint is currently anonymous.
- **ALT-003**: Add a model-callable `commit_location_entry` tool protected by a token returned from `prepare_location_entry`. Rejected because the model could immediately reuse the token and bypass the human approval requirement.
- **ALT-004**: Let the model call `ILocationService.CreateLocationAsync` directly. Rejected because prompt injection or model error could cause an unreviewed write.
- **ALT-005**: Remove Agent Framework immediately. Rejected because a provider feature flag supplies a low-risk rollback during SDK and deployment validation.
- **ALT-006**: Support distributed REST action confirmation in version 1. Rejected because it requires a shared pending-action store, distributed locking, and a new authenticated confirmation API beyond the stated chat-window goal.

## 4. Dependencies

- **DEP-001**: `GitHub.Copilot.SDK` 1.0.11, subject to dependency advisory review and compatibility validation.
- **DEP-002**: `Microsoft.Extensions.AI` abstractions used by `CopilotTool.DefineTool`, if not transitively available.
- **DEP-003**: Azure AI Foundry deployment supporting the OpenAI-compatible Responses API.
- **DEP-004**: `DefaultAzureCredential` and managed identity with permission to invoke the Foundry model.
- **DEP-005**: Executable GitHub Copilot runtime binary and a per-instance writable temporary directory in Linux Azure App Service.
- **DEP-006**: Existing `ILocationService`, `ILocationTypeService`, `ILocationLookupService`, repositories, and authentication service.
- **DEP-007**: Browser timezone discovery in the Blazor chat component.

## 5. Files

- **FILE-001**: `src/TravelTracker.Services/TravelTracker.Services.csproj` — add the SDK dependency.
- **FILE-002**: `src/TravelTracker.Data/Configuration/CopilotSdkSettings.cs` — add validated SDK and lifecycle options.
- **FILE-003**: `src/TravelTracker.Services/Interfaces/IAiChatProvider.cs` — add the provider abstraction.
- **FILE-004**: `src/TravelTracker.Services/Interfaces/IChatbotService.cs` — replace the tuple with typed chat and action contracts.
- **FILE-005**: `src/TravelTracker.Services/Interfaces/IPendingChatActionStore.cs` — define pending-action storage.
- **FILE-006**: `src/TravelTracker.Services/Models/ChatToolContracts.cs` — define tool and action DTOs.
- **FILE-007**: `src/TravelTracker.Services/Services/AgentFrameworkChatProvider.cs` — isolate legacy execution.
- **FILE-008**: `src/TravelTracker.Services/Services/CopilotSdkChatProvider.cs` — configure SDK sessions and Foundry BYOK.
- **FILE-009**: `src/TravelTracker.Services/Services/CopilotRuntimeService.cs` — own the singleton SDK runtime.
- **FILE-010**: `src/TravelTracker.Services/Services/ChatSessionManager.cs` — manage bounded user-bound sessions.
- **FILE-011**: `src/TravelTracker.Services/Services/TravelTrackerChatToolFactory.cs` — define restricted in-process tools.
- **FILE-012**: `src/TravelTracker.Services/Services/ChatActionExecutor.cs` — implement host-only confirmation and persistence.
- **FILE-013**: `src/TravelTracker.Services/Services/ChatbotService.cs` — orchestrate provider, session, tools, and typed results.
- **FILE-014**: `src/TravelTracker/Program.cs` — bind configuration and register services.
- **FILE-015**: `src/TravelTracker/appsettings.json` — add non-secret provider settings.
- **FILE-016**: `src/TravelTracker/Components/Pages/Chat.razor` — add timezone capture and confirmation UI.
- **FILE-017**: `src/TravelTracker/Controllers/ChatbotController.cs` — retain a read-only typed REST contract.
- **FILE-018**: `src/TravelTracker.Tests/Controllers/ChatbotControllerTests.cs` — update controller tests.
- **FILE-019**: `src/TravelTracker.Tests/Services/ChatbotServiceTests.cs` — add orchestration and provider tests.
- **FILE-020**: `src/TravelTracker.Tests/Services/TravelTrackerChatToolTests.cs` — add tool isolation and validation tests.
- **FILE-021**: `src/TravelTracker.Tests/Services/ChatActionExecutorTests.cs` — add confirmation and idempotency tests.
- **FILE-022**: `src/TravelTracker.Tests/Components/ChatTests.cs` — add confirmation UX tests.
- **FILE-023**: `Docs/CHATBOT_SETUP.md` — replace obsolete setup and architecture guidance.
- **FILE-024**: `Docs/MCP-SETUP.md` — document the read-only MCP boundary.

## 6. Testing

- **TEST-001**: Provider selection and invalid configuration fail predictably without exposing secrets.
- **TEST-002**: One runtime is reused and disposed correctly across concurrent sessions.
- **TEST-003**: Thread IDs cannot be reused across users and expired sessions cannot execute tools.
- **TEST-004**: No built-in shell, filesystem, or unrestricted network tool is available.
- **TEST-005**: Tool handlers resolve a fresh scope and can access data only for the session-bound user.
- **TEST-006**: Relative dates resolve correctly for the browser timezone at UTC date boundaries.
- **TEST-007**: Ambiguous location results cause clarification instead of a draft or write.
- **TEST-008**: Draft preparation validates `RV Park`, resolves Buffalo House details, and detects duplicates.
- **TEST-009**: No database write occurs before the host receives a Confirm click.
- **TEST-010**: A model cannot approve or execute its own pending action.
- **TEST-011**: Confirmed actions populate matching `LocationType` and `LocationTypeId`.
- **TEST-012**: Confirmation is single-use under retries and concurrent clicks.
- **TEST-013**: Cancelled, expired, cross-user, cross-thread, or tampered actions cannot write.
- **TEST-014**: The exact Buffalo House prompt creates one correct row only after confirmation.
- **TEST-015**: The legacy provider remains selectable and existing read-only questions continue to work.
- **TEST-016**: Linux App Service can start the runtime and call the configured Foundry Responses endpoint.

## 7. Risks & Assumptions

- **RISK-001**: The bundled Copilot runtime may not execute in Linux Azure App Service. Phase 1 is a hard go/no-go gate.
- **RISK-002**: A long-lived child runtime can consume significant memory or become unhealthy. The hosted service requires health metrics, bounded sessions, and restart handling.
- **RISK-003**: In-memory sessions and pending actions are not safe across instances. Version 1 requires one instance with affinity; shared storage and distributed locks are mandatory before scale-out.
- **RISK-004**: Public geocoding can return an incorrect or ambiguous park. The user must review resolved address and coordinates before confirmation.
- **RISK-005**: Relative dates can be off by one day without a valid browser timezone. The draft must display the resolved date and fallback timezone.
- **RISK-006**: The existing `LocationService.CreateLocationAsync` returns an empty `Location` on failure instead of throwing. `ChatActionExecutor` must treat a missing/non-positive created ID as failure and must not mark the action completed.
- **RISK-007**: SDK APIs and experimental diagnostics can change between releases. Pin 1.0.11 and validate upgrade notes before later updates.
- **RISK-008**: Provider rollback removes action capability. The UI must hide or disable action claims when the legacy provider is selected.
- **ASSUMPTION-001**: `RV Park` remains a valid seeded location type.
- **ASSUMPTION-002**: The authenticated internal user ID is available throughout the Blazor circuit.
- **ASSUMPTION-003**: Users accept an explicit confirmation step for persisted changes.
- **ASSUMPTION-004**: Azure AI Foundry remains the model backend; this plan changes orchestration rather than cloud model hosting.

## Rubber-Duck Review

The proposed architecture was reviewed by an independent rubber-duck agent against the current Travel Tracker code and SDK behavior. The review confirmed the provider boundary, restricted in-process tools, trusted user binding, scope-per-tool pattern, and target Buffalo House workflow. The plan incorporates every substantive correction:

- Host confirmation executes the write directly; the model never receives a commit credential or commit tool.
- REST chat is explicitly read-only in version 1 instead of pretending in-memory confirmation works across stateless scaled requests.
- Linux runtime execution and Foundry BYOK compatibility are a Phase 1 hard gate.
- Both location-type fields, browser timezone handling, ambiguity clarification, duplicate prevention, legacy cache removal, and rollback limitations have explicit tasks and tests.

Review result: the plan is executable and covers the target prompt after these corrections.

## 8. Related Specifications / Further Reading

- [Golden Copilot SDK provider](https://github.com/lluppesms/dadabase.demo/blob/main/src/web/Website/Services/CopilotSdkChatService.cs)
- [Golden provider-selection implementation](https://github.com/lluppesms/dadabase.demo/blob/main/src/web/Website/Helpers/AiServiceCollectionExtensions.cs)
- [GitHub Copilot SDK .NET documentation](https://github.com/github/copilot-sdk/blob/main/dotnet/README.md)
- [GitHub Copilot SDK 1.0.11 release](https://github.com/github/copilot-sdk/releases/tag/v1.0.11)
- [GitHub Copilot SDK custom tool implementation](https://github.com/github/copilot-sdk/blob/main/dotnet/src/CopilotTool.cs)
- [Travel Tracker chatbot service](../src/TravelTracker.Services/Services/ChatbotService.cs)
- [Travel Tracker location MCP tools](../src/TravelTracker.MCP/TravelTracker.MCP.Tools/Mcp/LocationTools.cs)
- [Travel Tracker chatbot setup](CHATBOT_SETUP.md)
