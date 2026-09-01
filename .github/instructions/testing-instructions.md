---
applyTo: "src/TravelTracker.Tests/**"
---

# Testing Instructions

Use these instructions when adding, updating, or reviewing automated tests in this repository.

## Testing stack and locations

- Use xUnit for .NET automated tests in this repository.
- Controller, data, and service tests live in `src/TravelTracker.Tests/`.
- Keep tests grouped by the production concern they validate.

## Unit testing guidance

Add or update unit and integration-style .NET tests when you change:

- controller or API endpoint behavior
- repository or data-access behavior
- model mapping or serialization behavior
- configuration-dependent logic
- error handling, null handling, or edge-case branching

Unit tests in this repository typically follow these patterns:

- prefer focused tests grouped under `Controllers/`, `Data/`, and `Services/`
- use `Fact` for single scenarios and theory-style coverage when repeated inputs make the test clearer
- mock or substitute external dependencies where practical
- prefer deterministic local test data over live external services
- preserve the repository's existing mock and test-data boundaries when they avoid unnecessary infrastructure coupling

Cover both happy paths and failure paths. If logic branches on missing configuration, invalid input, empty datasets, or repository failures, add tests for those outcomes instead of only testing the success case.

## Browser testing guidance

The current repository does not contain a `playwright/` test tree or Playwright configuration. Some workflow files still reference those paths. Do not document or invoke Playwright as an available local suite until the tests are restored or created and the workflow references are validated.

## Running tests

Common local commands:

```powershell
dotnet test .\src\TravelTracker.Tests\TravelTracker.Tests.csproj
dotnet test .\src\TravelTracker.sln
```

## Guidance for agents

When an agent is asked to test the running app, first verify that the required browser-test tooling and configuration exist. Use the general Playwright guidance only after a Travel Tracker browser-test surface has been established.

## Default expectations

- Extend existing test structure before creating new patterns
- Keep test names descriptive and behavior-focused
- Do not introduce live-environment dependencies when local data or mocks are sufficient
- Add browser coverage alongside .NET tests when a maintained browser-test suite exists and a change spans backend behavior and UI behavior
