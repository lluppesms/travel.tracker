# GitHub Copilot SDK Conversion

This folder contains the implementation-ready plan for replacing the Travel Tracker chatbot's direct Azure OpenAI/Agent Framework integration with `GitHub.Copilot.SDK` version `1.0.11` and adding safe, authenticated application actions.

## Documents

- [Research findings](research-findings.md): verified SDK capabilities, golden-repository comparison, and current Travel Tracker constraints.
- [Implementation plan](implementation-plan.md): requirements, architecture, file-level tasks, test matrix, rollout, and rollback criteria.
- [Rubber-duck review](rubber-duck-review.md): independent findings, required corrections, and final readiness verdict.

## Recommended Outcome

Use the Copilot SDK as an agent runtime over the existing Azure AI Foundry model through managed identity. Expose only explicit Travel Tracker tools from a restricted runtime. Keep authenticated user identity outside model arguments, and route all writes through an idempotent application action service.

The first production release requires confirmation before every database write. Streaming, automatic execution, and MCP mutation are follow-up work after the authenticated confirmation path, duplicate prevention, and audit records pass the release gate.

## Scope

This planning change does not modify application source, infrastructure, packages, or database schema. It does not create a branch or commit. Implementation begins only after this plan is approved.
