# Azure Setup Guide for Travel Tracker

This guide provides instructions for setting up the required Azure resources for the Travel Tracker application.

## Prerequisites

- Azure subscription (free or paid)
- Azure CLI installed (optional, can also use Azure Portal)
- Owner or Contributor access to the Azure subscription

## Required Azure Services

### 1. Azure AD (Entra ID) App Registration

**Purpose:** Provides authentication and user identity management.

#### Setup Steps:

1. **Navigate to Azure Portal**
   - Go to https://portal.azure.com
   - Sign in with your Azure account

2. **Create App Registration**
   - Navigate to "Azure Active Directory" (or "Microsoft Entra ID")
   - Select "App registrations" from the left menu
   - Click "New registration"
   - Configure:
     - **Name:** TravelTracker
     - **Supported account types:** Accounts in this organizational directory only (Single tenant)
     - **Redirect URI:** 
       - Platform: Web
       - URI: `https://localhost:5001/signin-oidc` (for local development)
       - Add additional URIs for deployed environments later
   - Click "Register"

3. **Configure Authentication**
   - In the app registration, go to "Authentication"
   - Under "Implicit grant and hybrid flows," enable:
     - ✅ ID tokens (used for implicit and hybrid flows)
   - Under "Front-channel logout URL," add: `https://localhost:5001/signout-oidc`
   - Click "Save"

4. **Create Client Secret**
   - Go to "Certificates & secrets"
   - Click "New client secret"
   - Add description: "TravelTracker Development"
   - Select expiration: 24 months (or as per your policy)
   - Click "Add"
   - **IMPORTANT:** Copy the secret value immediately - it won't be shown again!

5. **Configure API Permissions**
   - Go to "API permissions"
   - Verify these permissions are present:
     - Microsoft Graph > User.Read (Delegated)
   - If not, click "Add a permission" and add them
   - Click "Grant admin consent" (if you have admin rights)

6. **Get Configuration Values**
   - Go to "Overview" page
   - Copy these values for your appsettings.json:
     - **Application (client) ID:** `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx`
     - **Directory (tenant) ID:** `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx`
     - **Client secret:** (copied in step 4)

### 2. Azure Cosmos DB

**Purpose:** NoSQL database for storing user data and locations.

#### Setup Steps:

1. **Create Cosmos DB Account**
   - In Azure Portal, click "Create a resource"
   - Search for "Azure Cosmos DB"
   - Click "Create"
   - Select "Azure Cosmos DB for NoSQL"
   - Configure:
     - **Resource group:** Create new or use existing
     - **Account name:** traveltracker-cosmosdb (must be globally unique)
     - **Location:** Choose closest to your users
     - **Capacity mode:** Serverless (for development) or Provisioned throughput
   - Click "Review + Create" then "Create"

2. **Create Database and Containers**
   - Once deployed, go to the Cosmos DB account
   - Go to "Data Explorer"
   - Click "New Database"
     - **Database id:** TravelTrackerDB
     - Click "OK"
   - Create Containers:
     
     **Container 1: users**
     - Database: TravelTrackerDB
     - Container id: users
     - Partition key: /id
     - Click "OK"
     
     **Container 2: locations**
     - Database: TravelTrackerDB
     - Container id: locations
     - Partition key: /userId
     - Click "OK"
     
     **Container 3: nationalparks**
     - Database: TravelTrackerDB
     - Container id: nationalparks
     - Partition key: /state
     - Click "OK"

3. **Get Connection String**
   - Go to "Keys" in the left menu
   - Copy the "PRIMARY CONNECTION STRING"
   - This will be used in appsettings.json

### 3. Azure Maps

**Purpose:** Provides interactive map visualization.

#### Setup Steps:

1. **Create Azure Maps Account**
   - In Azure Portal, click "Create a resource"
   - Search for "Azure Maps"
   - Click "Create"
   - Configure:
     - **Resource group:** Use same as Cosmos DB
     - **Name:** traveltracker-maps
     - **Pricing tier:** S0 (Standard, pay-as-you-go) or S1
     - **Location:** Choose closest to your users
   - Accept terms and conditions
   - Click "Review + Create" then "Create"

2. **Get Subscription Key**
   - Once deployed, go to the Azure Maps account
   - Go to "Authentication" in the left menu
   - Copy the "Primary Key" under "Shared Key Authentication"
   - This will be used in appsettings.json

### 4. Application Insights (Optional but Recommended)

**Purpose:** Application monitoring and diagnostics.

#### Setup Steps:

1. **Create Application Insights**
   - In Azure Portal, click "Create a resource"
   - Search for "Application Insights"
   - Click "Create"
   - Configure:
     - **Resource group:** Use same as other resources
     - **Name:** traveltracker-insights
     - **Region:** Choose same as other resources
     - **Resource Mode:** Workspace-based
   - Click "Review + Create" then "Create"

2. **Get Instrumentation Key**
   - Once deployed, go to Application Insights
   - Go to "Properties" in the left menu
   - Copy the "Instrumentation Key"
   - This will be used for monitoring

## Configuration

### Update appsettings.json

After creating all Azure resources, update your `appsettings.json` file:

```json
{
  "CosmosDb": {
    "ConnectionString": "<YOUR_COSMOS_DB_CONNECTION_STRING>",
    "DatabaseName": "TravelTrackerDB",
    "UsersContainerName": "users",
    "LocationsContainerName": "locations",
    "NationalParksContainerName": "nationalparks"
  },
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "<YOUR_DOMAIN>.onmicrosoft.com",
    "TenantId": "<YOUR_TENANT_ID>",
    "ClientId": "<YOUR_CLIENT_ID>",
    "ClientSecret": "<YOUR_CLIENT_SECRET>",
    "CallbackPath": "/signin-oidc"
  },
  "AzureMaps": {
    "SubscriptionKey": "<YOUR_AZURE_MAPS_PRIMARY_KEY>",
    "ClientId": ""
  },
  "ApplicationInsights": {
    "InstrumentationKey": "<YOUR_APP_INSIGHTS_KEY>"
  }
}
```

### Environment Variables (Alternative)

For better security, especially in production, use environment variables or Azure Key Vault:

```bash
export CosmosDb__ConnectionString="<value>"
export AzureAd__ClientSecret="<value>"
export AzureMaps__SubscriptionKey="<value>"
```

### appsettings.Development.json

For local development, create an `appsettings.Development.json` file (already in .gitignore):

```json
{
  "CosmosDb": {
    "ConnectionString": "<LOCAL_OR_DEV_CONNECTION_STRING>"
  },
  "AzureAd": {
    "ClientSecret": "<DEV_CLIENT_SECRET>"
  },
  "AzureMaps": {
    "SubscriptionKey": "<DEV_SUBSCRIPTION_KEY>"
  }
}
```

## Security Best Practices

### Do NOT commit secrets to source control!

1. **Never commit** `appsettings.Development.json` or any file containing secrets
2. Use `.gitignore` to exclude sensitive configuration files
3. For production, use:
   - Azure Key Vault
   - Managed Identities
   - Environment variables
   - Azure App Configuration

### Managed Identity (Production)

For production deployments, configure Managed Identity:

1. **Enable Managed Identity on App Service**
   - Go to your App Service
   - Navigate to "Identity"
   - Enable "System assigned" identity
   - Copy the Object ID

2. **Grant Access to Resources**
   - Cosmos DB: Assign "Cosmos DB Account Reader" role
   - Key Vault: Add access policy with Get/List secrets permissions
   - Azure Maps: Assign appropriate role

3. **Update Code to Use Managed Identity**
   ```csharp
   services.AddSingleton<CosmosClient>(sp =>
   {
       var credential = new DefaultAzureCredential();
       return new CosmosClient("<account-endpoint>", credential);
   });
   ```

## Testing the Setup

### Test Authentication

1. Run the application locally
2. Navigate to any protected page (e.g., `/locations`)
3. You should be redirected to Microsoft login
4. After login, you should see your name in the navigation bar

### Test Cosmos DB

1. Navigate to `/locations` page
2. Try creating a new location
3. Verify it appears in the list
4. Check Data Explorer in Azure Portal to see the data

### Test Azure Maps

1. Navigate to `/map` page
2. If Azure Maps is configured, you should see an interactive map
3. Your locations should appear as markers on the map
4. Try the different view modes (Date Range, State Overview, National Parks)

## Troubleshooting

### Authentication Issues

- **Error: AADSTS50011** - The redirect URI doesn't match
  - Solution: Add the correct redirect URI in Azure AD app registration
  
- **Error: Unauthorized** - User not authenticated
  - Solution: Check that authentication middleware is properly configured in Program.cs

### Cosmos DB Issues

- **Error: Resource Not Found** - Container doesn't exist
  - Solution: Create the containers as specified in setup steps

- **Error: Insufficient permissions** - Connection string doesn't have right permissions
  - Solution: Use the PRIMARY CONNECTION STRING from Keys section

### Azure Maps Issues

- **Map not loading** - Subscription key is invalid
  - Solution: Verify the subscription key in appsettings.json matches Azure Portal

- **403 Forbidden** - Insufficient permissions
  - Solution: Check your Azure Maps pricing tier and subscription status

## Cost Estimation

Based on the architecture:

### Development Environment (~$24/month)
- Cosmos DB Serverless: ~$5
- Azure Maps S0: ~$5
- Application Insights: ~$5
- Azure AD: Free
- App Service B1: ~$13

### Production Environment (~$166/month)
- Cosmos DB (400 RU/s): ~$24
- Azure Maps S0: ~$20
- Application Insights: ~$50
- Azure AD: Free
- App Service S1: ~$70

**Note:** Actual costs may vary based on usage patterns.

## Next Steps

1. ✅ Create Azure resources as described above
2. ✅ Update appsettings.json with credentials
3. ✅ Test the application locally
4. Move to Phase 4: Infrastructure as Code
5. Deploy to Azure App Service
6. Set up CI/CD pipeline

## Support

For issues or questions:
- Check the Azure documentation
- Review the application logs in Application Insights
- Open an issue in the repository
- Contact the development team

---

**Last Updated:** October 16, 2025  
**Version:** 1.0
