# Travel Tracker Web Application Deployment Guide

Travel Tracker currently supports deployment to Azure App Service through Bicep, GitHub Actions, Azure DevOps, or Azure Developer CLI.

## Supported Architecture

The active Bicep composition in `infra/Bicep/main.bicep` can deploy:

- Azure App Service and App Service Plan
- Azure SQL Database
- Azure Key Vault
- Azure Storage
- Application Insights and Log Analytics
- SignalR
- system-assigned or optional user-assigned managed identity

The `deploymentType` parameter accepts labels including `containerapp` and `functionapp`, but the current repository contains no Container Apps modules, function project, Dockerfile, or container deployment workflows. Use `webapp` for deployment. Use `all` only after reviewing the current Bicep conditions; it currently includes the App Service path.

## Prerequisites

- Azure subscription and permission to create the required resources
- Azure CLI for local deployment
- .NET 10 SDK for local builds
- GitHub Actions OIDC credentials or an Azure DevOps service connection for CI/CD
- environment-specific repository, environment, or variable-group settings

Do not store connection strings, API keys, client secrets, or passwords in source control.

## GitHub Actions

The primary workflow is `.github/workflows/2.1-bicep-build-deploy-webapp.yml`. It can:

1. Load repository configuration.
2. Run security scans.
3. deploy `infra/Bicep/main.bicep` with `deploymentType: webapp`.
4. Build and test `src/TravelTracker/TravelTracker.csproj`.
5. Deploy the build artifact to App Service.
6. Optionally invoke the smoke-test workflow.

Set up credentials and variables as described in `.github/CreateGitHubSecrets.md` and `.github/workflows-readme.md` before running the workflow.

Infrastructure-only deployment is available through `.github/workflows/1-deploy-bicep.yml`. Database deployment is handled separately by `.github/workflows/4-build-deploy-dacpac.yml`.

## Azure DevOps

The current Azure DevOps entry pipelines are under `.azdo/pipelines/`:

- `infra-and-webapp-pipeline.yml`
- `infra-only-pipeline.yml`
- `build-webapp-only-pipeline.yml`
- `deploy-webapp-only-pipeline.yml`
- `pr-pipeline.yml`
- `scan-pipeline.yml`
- `smoke-test-pipeline.yml`

Shared steps and templates live under `.azdo/pipelines/pipes/`; source and environment values live under `.azdo/pipelines/vars/`. Verify variable-group names and values against those templates because the Azure DevOps readme still contains copied sample values.

## Local Bicep Deployment

Run the following from the repository root after authenticating and selecting the correct subscription:

```powershell
$resourceGroup = "rg-travel-tracker-dev"
$location = "centralus"
$appName = "traveltracker"

az group create --name $resourceGroup --location $location
az deployment group create `
  --name "traveltracker-$(Get-Date -Format 'yyyyMMdd-HHmmss')" `
  --resource-group $resourceGroup `
  --template-file .\infra\Bicep\main.bicep `
  --parameters `
    appName=$appName `
    environmentCode=dev `
    deploymentType=webapp
```

For repeatable environment settings, use `infra/Bicep/main.bicepparam` and supply required secure values through the deployment environment.

## Build the Application

```powershell
dotnet restore .\src\TravelTracker.sln
dotnet build .\src\TravelTracker.sln -c Release
dotnet test .\src\TravelTracker.Tests\TravelTracker.Tests.csproj -c Release
dotnet publish .\src\TravelTracker\TravelTracker.csproj -c Release -o .\publish
```

The CI/CD templates package and deploy the publish output. Prefer those templates over hand-creating deployment archives.

## Deploy the Database

The schema authority is `src/sql.database/sql.database.sqlproj`. Build and deploy its DACPAC separately from the web app:

```powershell
dotnet build .\src\sql.database\sql.database.sqlproj -c Release
```

Use `.github/workflows/4-build-deploy-dacpac.yml` or the corresponding Azure DevOps database templates for controlled environment deployment. Database identities require the permissions described in `.github/workflows-readme.md`.

## Application Configuration

The deployed app uses these primary configuration sections:

- `SqlServer:ConnectionString`
- `AzureAd`
- `AzureMaps`
- `AzureAIFoundry`
- API-key settings

The app only registers repositories and application services when `SqlServer:ConnectionString` is present. Entra ID authentication is enabled only when tenant and client IDs are configured.

## Validation

After deployment:

1. Confirm the App Service reports healthy startup logs.
2. Open `/api/swagger` and verify the API document loads.
3. Exercise a read-only endpoint with the expected authentication or API key.
4. Confirm the database schema is present under `Travel`.
5. Review Application Insights for startup or dependency failures.

The smoke-test workflows currently reference a missing `playwright/` tree. Do not treat those jobs as valid deployment evidence until the browser tests are restored or the workflows are revised.

**Last updated:** September 2026
**Repository:** lluppesms/travel.tracker
