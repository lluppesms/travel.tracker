# Travel Tracker Deployment Quick Reference

## Build and Test

```powershell
dotnet restore .\src\TravelTracker.sln
dotnet build .\src\TravelTracker.sln -c Release
dotnet test .\src\TravelTracker.Tests\TravelTracker.Tests.csproj -c Release
```

## Deploy App Service Infrastructure

```powershell
$resourceGroup = "rg-travel-tracker-dev"
$location = "centralus"
$appName = "traveltracker"

az group create --name $resourceGroup --location $location
az deployment group create `
  --resource-group $resourceGroup `
  --template-file .\infra\Bicep\main.bicep `
  --parameters appName=$appName environmentCode=dev deploymentType=webapp
```

## Publish the Web App

```powershell
dotnet publish .\src\TravelTracker\TravelTracker.csproj -c Release -o .\publish
```

Use the repository CI/CD templates to package and deploy the publish output.

## GitHub Actions

| Purpose | Workflow |
| --- | --- |
| Infrastructure only | `.github/workflows/1-deploy-bicep.yml` |
| Infrastructure, build, and App Service deploy | `.github/workflows/2.1-bicep-build-deploy-webapp.yml` |
| DACPAC build and deploy | `.github/workflows/4-build-deploy-dacpac.yml` |
| SQL script execution | `.github/workflows/5-run-sql-script.yml` |
| Pull-request build and scan | `.github/workflows/6-pr-scan-build.yml` |
| Security scan and SBOM | `.github/workflows/7-scan-code.yml` |

## Azure DevOps

| Purpose | Pipeline |
| --- | --- |
| Infrastructure and App Service | `.azdo/pipelines/infra-and-webapp-pipeline.yml` |
| Infrastructure only | `.azdo/pipelines/infra-only-pipeline.yml` |
| Build web app | `.azdo/pipelines/build-webapp-only-pipeline.yml` |
| Deploy an existing web artifact | `.azdo/pipelines/deploy-webapp-only-pipeline.yml` |
| Pull-request validation | `.azdo/pipelines/pr-pipeline.yml` |
| Security scan | `.azdo/pipelines/scan-pipeline.yml` |

## Key Files

- `infra/Bicep/main.bicep`: main resource composition
- `infra/Bicep/main.bicepparam`: shared deployment parameters
- `infra/Bicep/resourcenames.bicep`: Azure resource naming
- `infra/Bicep/modules/webapp/`: App Service modules
- `src/TravelTracker/TravelTracker.csproj`: web project
- `src/TravelTracker.Tests/TravelTracker.Tests.csproj`: test project
- `src/sql.database/sql.database.sqlproj`: DACPAC project
- `.github/CreateGitHubSecrets.md`: GitHub credentials and variables

## Required Application Settings

```text
SqlServer__ConnectionString
AzureAd__TenantId
AzureAd__ClientId
AzureMaps__SubscriptionKey
AzureAIFoundry__Endpoint
AzureAIFoundry__DeploymentName
ApiKey
```

Only SQL configuration is required for repository and service registration. Authentication, maps, AI, and API-key features require their corresponding settings.

## Deployment Limitations

- The verified hosting target is Azure App Service.
- The repository has no Container Apps modules or Dockerfile.
- The repository has no Azure Functions project.
- Smoke-test workflows reference a missing `playwright/` directory and require repair before use.

## Useful Diagnostics

```powershell
az webapp log tail --name <web-app-name> --resource-group <resource-group>
az deployment group list --resource-group <resource-group> --output table
az deployment group show --resource-group <resource-group> --name <deployment-name>
```

Swagger is available at `/api/swagger` after a successful application startup.
