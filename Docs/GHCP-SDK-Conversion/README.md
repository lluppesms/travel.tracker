# GitHub Copilot SDK Conversion

This folder contains the implementation-ready plan for replacing the Travel Tracker chatbot's direct Azure OpenAI/Agent Framework integration with `GitHub.Copilot.SDK` version `1.0.11` and adding safe, authenticated application actions.

## Documents

- [Research findings](research-findings.md): verified SDK capabilities, golden-repository comparison, and current Travel Tracker constraints.
- [Implementation plan](implementation-plan.md): requirements, architecture, file-level tasks, test matrix, rollout, and rollback criteria.
- `rubber-duck-review.md`: independent review findings and plan corrections. This file is added after the first plan validation pass.

## Recommended Outcome

Use the Copilot SDK as an agent runtime over the existing Azure AI Foundry model through managed identity. Expose only explicit Travel Tracker tools from a restricted runtime. Keep authenticated user identity outside model arguments, and route all writes through an idempotent application action service.

The first production release should require confirmation before a database write. A personal deployment may opt into automatic execution through configuration after the confirmation path, authorization checks, duplicate detection, and audit records have passed the acceptance suite.

## Scope

This planning change does not modify application source, infrastructure, packages, or database schema. It does not create a branch or commit. Implementation begins only after this plan is approved.
