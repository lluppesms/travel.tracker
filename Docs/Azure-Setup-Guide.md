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

### 2. Azure SQL Database

**Purpose:** SQL Server database for storing user data and locations.

#### Setup Steps:

1. **Create SQL Server**
   - In Azure Portal, click "Create a resource"
   - Search for "SQL Server"
   - Click "Create"
   - Configure:
     - **Resource group:** Create new or use existing
     - **Server name:** traveltracker-sqlserver (must be globally unique)
     - **Location:** Choose closest to your users
     - **Authentication method:** Use SQL authentication
     - **Server admin login:** Choose a username (e.g., sqladmin)
     - **Password:** Create a strong password
   - Click "Review + Create" then "Create"

2. **Configure Firewall**
   - Once deployed, go to the SQL server
   - Go to "Networking" (or "Firewalls and virtual networks")
   - Configure firewall rules:
     - **Allow Azure services:** Yes
     - **Add your client IP:** Click "Add client IP" to allow your development machine
     - For production, use private endpoints or specific IP ranges
   - Click "Save"

3. **Create Database**
   - In the SQL server, go to "SQL databases"
   - Click "Create database" (or "Add database")
   - Configure:
     - **Database name:** TravelTrackerDB
     - **Compute + storage:** Choose appropriate tier
       - For development: Basic or S0
       - For production: S1 or higher based on needs
   - Click "Review + Create" then "Create"

4. **Get Connection String**
   - Go to the database (TravelTrackerDB)
   - Go to "Connection strings" in the left menu
   - Copy the "ADO.NET (SQL authentication)" connection string
   - Replace `{your_password}` with the password you created in step 1
   - This will be used in appsettings.json

**Alternative: SQL Server LocalDB or Express for Development**
- For local development, you can use SQL Server Express or LocalDB instead
- See [DatabaseSetup.md](DatabaseSetup.md) for detailed instructions

### 3. Azure Maps

**Purpose:** Provides interactive map visualization.

#### Setup Steps:

1. **Create Azure Maps Account**
   - In Azure Portal, click "Create a resource"
   - Search for "Azure Maps"
   - Click "Create"
   - Configure:
     - **Resource group:** Use same as SQL Server
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
  "SqlServer": {
    "ConnectionString": "Server=tcp:traveltracker-sqlserver.database.windows.net,1433;Database=TravelTrackerDB;User ID=sqladmin;Password={your_password};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
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
export SqlServer__ConnectionString="<value>"
export AzureAd__ClientSecret="<value>"
export AzureMaps__SubscriptionKey="<value>"
```

### appsettings.Development.json

For local development, create an `appsettings.Development.json` file (already in .gitignore):

```json
{
  "SqlServer": {
    "ConnectionString": "Server=localhost;Database=TravelTrackerDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
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
   - SQL Server: Add managed identity as a database user with appropriate permissions
   - Key Vault: Add access policy with Get/List secrets permissions
   - Azure Maps: Assign appropriate role

3. **Update Connection String for Managed Identity**
   - For Azure SQL Database with Managed Identity:
     ```
     Server=tcp:traveltracker-sqlserver.database.windows.net,1433;Database=TravelTrackerDB;Authentication=Active Directory Default;Encrypt=True;
     ```
   - The application will use DefaultAzureCredential to authenticate

## Testing the Setup

### Test Authentication

1. Run the application locally
2. Navigate to any protected page (e.g., `/locations`)
3. You should be redirected to Microsoft login
4. After login, you should see your name in the navigation bar

### Test SQL Server Database

1. Ensure the database migration has been applied:
   ```bash
   cd src/TravelTracker.Data
   dotnet ef database update --startup-project ../TravelTracker
   ```
2. Navigate to `/locations` page
3. Try creating a new location
4. Verify it appears in the list
5. Check the database using SQL Server Management Studio or Azure Portal to see the data

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

### SQL Server Issues

- **Error: Cannot open database** - Database doesn't exist
  - Solution: Run database migrations using `dotnet ef database update`

- **Error: Login failed** - Invalid credentials
  - Solution: Verify username and password in connection string

- **Error: Cannot connect to server** - Firewall blocking connection
  - Solution: Add your IP address to Azure SQL Server firewall rules

### Azure Maps Issues

- **Map not loading** - Subscription key is invalid
  - Solution: Verify the subscription key in appsettings.json matches Azure Portal

- **403 Forbidden** - Insufficient permissions
  - Solution: Check your Azure Maps pricing tier and subscription status

## Cost Estimation

Based on the architecture:

### Development Environment (~$23/month)
- Azure SQL Database (Basic): ~$5
- Azure Maps S0: ~$5
- Application Insights: ~$5
- Azure AD: Free
- App Service B1: ~$13

### Production Environment (~$154/month)
- Azure SQL Database (S1): ~$30
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
