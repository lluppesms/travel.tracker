// --------------------------------------------------------------------------------
// Main Bicep file that creates all of the Azure Resources for one environment
// --------------------------------------------------------------------------------
// To deploy this Bicep manually:
// 	 az login
//   az account set --subscription <subscriptionId>
//   az deployment group create -n "manual-$(Get-Date -Format 'yyyyMMdd-HHmmss')" --resource-group rg_traveltracker_test --template-file 'main.bicep' --parameters appName=xxx-traveltracker-test environmentCode=demo keyVaultOwnerUserId=xxxxxxxx-xxxx-xxxx
// --------------------------------------------------------------------------------
param appName string = ''
param environmentCode string = 'azd'
param location string = resourceGroup().location

param storageSku string = 'Standard_LRS'
param webSiteSku string = 'B1'

param apiKey string = ''

param adInstance string = environment().authentication.loginEndpoint // 'https://login.microsoftonline.com/'
param adDomain string = ''
param adTenantId string = ''
param adClientId string = ''
@secure()
param adClientSecret string = ''
param adCallbackPath string = '/signin-oidc'

@secure()
param azureMapsSubscriptionKey string = ''
param azureMapsClientId string = ''

param azureAIFoundryEndpoint string = ''
@secure()
param azureAIFoundryApiKey string = ''
param azureAIFoundryDeploymentName string = ''

param appDataSource string = 'JSON'
param appSwaggerEnabled string = 'true'
param servicePlanName string = ''
param servicePlanResourceGroupName string = '' // if using an existing service plan in a different resource group
param webAppKind string = 'linux' // 'linux' or 'windows'

param existingSqlServerName string = ''
param existingSqlServerResourceGroupName string = ''

param sqlDatabaseName string = 'traveltracker'
@allowed(['Basic','Standard','Premium','BusinessCritical','GeneralPurpose'])
param sqlSkuTier string = 'GeneralPurpose'
param sqlSkuFamily string = 'Gen5'
param sqlSkuName string = 'GP_S_Gen5'
param sqlAdminUser string = ''
@secure()
param sqlAdminPassword string = ''

param sqlADAdminLoginUserId string = ''
param sqlADAdminLoginUserSid string = ''
param sqlADAdminLoginTenantId string = ''

param runDateTime string = utcNow()

// --------------------------------------------------------------------------------
var deploymentSuffix = '-${runDateTime}'
var commonTags = {         
  LastDeployed: runDateTime
  Application: appName
  Environment: environmentCode
}
var resourceGroupName = resourceGroup().name
// var resourceToken = toLower(uniqueString(resourceGroup().id, location))

// --------------------------------------------------------------------------------
module resourceNames 'resourcenames.bicep' = {
  name: 'resourcenames${deploymentSuffix}'
  params: {
    appName: appName
    environmentCode: environmentCode
  }
}
// --------------------------------------------------------------------------------
module logAnalyticsWorkspaceModule './modules/monitor/loganalytics.bicep' = {
  name: 'logAnalytics${deploymentSuffix}'
  params: {
    newLogAnalyticsName: resourceNames.outputs.logAnalyticsWorkspaceName
    location: location
    tags: commonTags
  }
}

// --------------------------------------------------------------------------------
module storageModule './modules/storage/storage-account.bicep' = {
  name: 'storage${deploymentSuffix}'
  params: {
    storageSku: storageSku
    storageAccountName: resourceNames.outputs.storageAccountName
    location: location
    commonTags: commonTags
  }
}

// --------------------------------------------------------------------------------
module identity './modules/iam/identity.bicep' = {
  name: 'app-identity${deploymentSuffix}'
  params: {
    identityName: resourceNames.outputs.userAssignedIdentityName
    location: location
  }
}

// --------------------------------------------------------------------------------
module sqlDbModule './modules/database/sqlserver.bicep' = {
  name: 'sql-server${deploymentSuffix}'
  params: {
    sqlServerName: resourceNames.outputs.sqlServerName
    sqlDBName: sqlDatabaseName
    existingSqlServerName: existingSqlServerName
    existingSqlServerResourceGroupName: existingSqlServerResourceGroupName
    sqlSkuTier: sqlSkuTier
    sqlSkuName: sqlSkuName
    sqlSkuFamily: sqlSkuFamily
    mincores: 1
    autopause: 60
    location: location
    commonTags: commonTags
    adAdminUserId: sqlADAdminLoginUserId
    adAdminUserSid: sqlADAdminLoginUserSid
    adAdminTenantId: sqlADAdminLoginTenantId
    //userAssignedIdentityPrincipalId: identity.outputs.managedIdentityPrincipalId
    userAssignedIdentityResourceId: identity.outputs.managedIdentityId
    sqlAdminUser:sqlAdminUser
    sqlAdminPassword: sqlAdminPassword
    workspaceId: logAnalyticsWorkspaceModule.outputs.logAnalyticsWorkspaceId
    // storageAccountName: resourceNames.outputs.storageAccountName
  }
}

// --------------------------------------------------------------------------------
module appServicePlanModule './modules/webapp/websiteserviceplan.bicep' = {
  name: 'appService${deploymentSuffix}'
  params: {
    location: location
    commonTags: commonTags
    sku: webSiteSku
    environmentCode: environmentCode
    appServicePlanName: servicePlanName == '' ? resourceNames.outputs.webSiteAppServicePlanName : servicePlanName
    existingServicePlanName: servicePlanName
    existingServicePlanResourceGroupName: servicePlanResourceGroupName
    webAppKind: webAppKind
  }
}

// --------------------------------------------------------------------------------
module webSiteModule './modules/webapp/website.bicep' = {
  name: 'webSite${deploymentSuffix}'
  params: {
    webSiteName: resourceNames.outputs.webSiteName
    location: location
    commonTags: commonTags
    environmentCode: environmentCode
    webAppKind: webAppKind
    workspaceId: logAnalyticsWorkspaceModule.outputs.logAnalyticsWorkspaceId
    userAssignedIdentityId: identity.outputs.managedIdentityId
    appServicePlanName: appServicePlanModule.outputs.name
    appServicePlanResourceGroupName: appServicePlanModule.outputs.resourceGroupName
  }
}

// In a Linux app service, any nested JSON app key like AppSettings:MyKey needs to be 
// configured in App Service as AppSettings__MyKey for the key name. 
// In other words, any : should be replaced by __ (double underscore).
// NOTE: See https://learn.microsoft.com/en-us/azure/app-service/configure-common?tabs=portal  
module webSiteAppSettingsModule './modules/webapp/websiteappsettings.bicep' = {
  name: 'webSiteAppSettings${deploymentSuffix}'
  params: {
    webAppName: webSiteModule.outputs.name
    appInsightsKey: webSiteModule.outputs.appInsightsKey
    customAppSettings: {
      AppSettings__AppInsights_InstrumentationKey: webSiteModule.outputs.appInsightsKey
      APPLICATIONINSIGHTS_CONNECTION_STRING: webSiteModule.outputs.appInsightsConnectionString
      AppSettings__EnvironmentName: environmentCode
      AppSettings__EnableSwagger: appSwaggerEnabled
      AppSettings__DataSource: appDataSource
      
      ApiKey: apiKey

      SqlServer__ConnectionString: 'Server=tcp:${sqlDbModule.outputs.serverName}${environment().suffixes.sqlServerHostname},1433;Initial Catalog=${sqlDbModule.outputs.databaseName};Authentication=Active Directory Default;Encrypt=True;Connection Timeout=30;'

      AzureAD__Instance: adInstance
      AzureAD__Domain: adDomain
      AzureAD__TenantId: adTenantId
      AzureAD__ClientId: adClientId
      AzureAD__ClientSecret: adClientSecret
      AzureAD__CallbackPath: adCallbackPath

      AzureMaps__SubscriptionKey: azureMapsSubscriptionKey
      AzureMaps__ClientId: azureMapsClientId

      AzureAIFoundry__Endpoint: azureAIFoundryEndpoint
      AzureAIFoundry__ApiKey: azureAIFoundryApiKey
      AzureAIFoundry__DeploymentName: azureAIFoundryDeploymentName
    }
  }
}

// --------------------------------------------------------------------------------
output SUBSCRIPTION_ID string = subscription().subscriptionId
output RESOURCE_GROUP_NAME string = resourceGroupName
output HOST_NAME string = webSiteModule.outputs.hostName
