# Rubber-Duck Review

## Review Method

An independent architecture agent reviewed the first draft against the current Travel Tracker source, GitHub Copilot SDK `1.0.11`, and the Buffalo House request. It was asked to falsify SDK, lifecycle, authorization, idempotency, geocoding, date, UI/API, deployment, testing, and rollback assumptions.

## Initial Verdict

**Not ready.** The first draft had three blocking defects:

1. The action ledger stored a hash and summary but no executable canonical command.
2. Chat identity could remain client-selectable through the current query/global-key behavior.
3. Exactly-once behavior did not define one transaction spanning action claim, duplicate enforcement, location insert, and completion.

## Additional Material Findings

- The current geocoder requests only one result and cannot implement the planned ambiguity contract; an exact Buffalo House query may return no result.
- Relative-date correctness depended on model output rather than deterministic server code.
- Confirmation was incorrectly coupled to the selected chat provider and could be stranded by rollback.
- DI lifetimes needed an explicit singleton/scoped matrix and build-time scope validation.
- Session disposal preserves SDK state; ephemeral sessions require `DeleteSessionAsync`.
- Bicep needed one enforced identity mode, cross-resource-group Foundry RBAC, one worker, writable `COPILOT_HOME`, and separate runtime-asset validation.
- First-release streaming and automatic execution enlarged risk without helping prove the core workflow.
- Live tests and rollback thresholds were optional or undefined.

## Revisions Applied

- Added a Data Protection encrypted, versioned canonical action payload, hash, unique idempotency key, retention/purge policy, rowversion, and unique location/action reference.
- Defined one serializable SQL transaction and crash/retry semantics.
- Required authentication and principal-derived identity for every assistant entry point; prohibited global-key impersonation.
- Moved confirm/cancel to a provider-neutral action service and endpoint.
- Added deterministic application date resolution and model/server disagreement checks.
- Replaced first-result geocoding with ranked candidates, broader fallback, provider evidence/divergence, opaque expiring candidate IDs, rate limits, caching, and clarification.
- Added an explicit DI lifetime matrix and scope validation.
- Added SDK session deletion, startup cleanup, resource caps, and fixed runtime/session limits.
- Narrowed the first release to confirmation-only, non-streaming behavior. Deferred streaming, `AutoExecute`, and MCP mutation.
- Enforced one user-assigned identity, cross-resource-group least-privilege Foundry RBAC, one App Service worker, writable `/tmp` state, and Linux asset/permission checks.
- Made the pre-production live run mandatory and assigned numeric reliability, latency, and resource gates.

## Buffalo House Walkthrough

The first draft failed because the geocoder could return no candidate, the model owned date interpretation, and the pending action was not executable after restart.

The revised plan passes at the design level:

1. Server date resolution maps `Yesterday` to `2026-08-31` in `America/Chicago`.
2. `RV Park` resolves against seeded location types.
3. Candidate lookup ranks broader provider results or asks the user to choose; unresolved ambiguity produces no action.
4. Preparation stores an encrypted canonical command bound to the authenticated user and thread.
5. Confirmation reloads and verifies that command and performs one atomic transaction.
6. Success is reported only for a persisted nonzero location ID; retries return the original result.

## Confirmed Decisions

- Pin stable `GitHub.Copilot.SDK` `1.0.11`.
- Use Foundry `/openai/v1` responses configuration with a managed-identity bearer provider.
- Own one hosted client, isolate sessions, serialize turns, and create a DI scope per tool invocation.
- Use empty client mode, an exact four-tool allowlist, no model-visible user ID, and content-safe telemetry.
- Keep Agent Framework as a provider rollback while retaining the provider-neutral action ledger and confirmation path.

## Final Verdict

**Ready with implementation gates.** The plan is internally implementation-ready, but production enablement is conditional on the mandatory Phase 6 live `1.0.11` acceptance run. This planning-only change does not claim that the exact Buffalo House geocoder result or App Service runtime behavior has been executed; those are explicit release gates.