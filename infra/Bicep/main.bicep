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
param webAppKind string = 'linux' // 'linux' or 'windows'

param sqlDatabaseName string = 'traveltracker'
@allowed(['Basic','Standard','Premium','BusinessCritical','GeneralPurpose'])
param sqlSkuTier string = 'GeneralPurpose'
param sqlSkuFamily string = 'Gen5'
param sqlSkuName string = 'GP_S_Gen5'
param sqlAdminUser string = ''
@secure()
param sqlAdminPassword string = ''

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
module sqlDbModule './modules/database/sqlserver.bicep' = {
  name: 'sql-server${deploymentSuffix}'
  params: {
    sqlServerName: resourceNames.outputs.sqlServerName
    sqlDBName: sqlDatabaseName
    sqlSkuTier: sqlSkuTier
    sqlSkuName: sqlSkuName
    sqlSkuFamily: sqlSkuFamily
    mincores: 1
    autopause: 60
    location: location
    commonTags: commonTags
    // adAdminUserId: adminLoginUserId
    // adAdminUserSid: adminLoginUserSid
    // adAdminTenantId: adminLoginTenantId
    sqlAdminUser:sqlAdminUser
    sqlAdminPassword: sqlAdminPassword
    workspaceId: logAnalyticsWorkspaceModule.outputs.logAnalyticsWorkspaceId
    // storageAccountName: resourceNames.outputs.storageAccountName
  }
}


module appServicePlanModule './modules/webapp/websiteserviceplan.bicep' = {
  name: 'appService${deploymentSuffix}'
  params: {
    location: location
    commonTags: commonTags
    sku: webSiteSku
    environmentCode: environmentCode
    appServicePlanName: servicePlanName == '' ? resourceNames.outputs.webSiteAppServicePlanName : servicePlanName
    existingServicePlanName: servicePlanName
    webAppKind: webAppKind
  }
}


module webSiteModule './modules/webapp/website.bicep' = {
  name: 'webSite${deploymentSuffix}'
  params: {
    webSiteName: resourceNames.outputs.webSiteName
    location: location
    commonTags: commonTags
    environmentCode: environmentCode
    webAppKind: webAppKind
    workspaceId: logAnalyticsWorkspaceModule.outputs.logAnalyticsWorkspaceId
    appServicePlanName: appServicePlanModule.outputs.name
  }
}

// In a Linux app service, any nested JSON app key like AppSettings:MyKey needs to be 
// configured in App Service as AppSettings__MyKey for the key name. 
// In other words, any : should be replaced by __ (double underscore).
// NOTE: See https://learn.microsoft.com/en-us/azure/app-service/configure-common?tabs=portal  
var sqlConnectionString = '-${runDateTime}'
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

      SqlServer__ConnectionString:	'Server=${resourceNames.outputs.sqlServerName};Database=TravelTrackerDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true'

      AzureAD__Instance: adInstance
      AzureAD__Domain: adDomain
      AzureAD__TenantId: adTenantId
      AzureAD__ClientId: adClientId
      AzureAD__ClientSecret: adClientSecret
      AzureAD__CallbackPath: adCallbackPath

      AzureMaps_SubscriptionKey: azureMapsSubscriptionKey
      AzureMaps_ClientId: azureMapsClientId

      AzureAIFoundry_Endpoint: azureAIFoundryEndpoint
      AzureAIFoundry_ApiKey: azureAIFoundryApiKey
      AzureAIFoundry_DeploymentName: azureAIFoundryDeploymentName
    }
  }
}


output SUBSCRIPTION_ID string = subscription().subscriptionId
output RESOURCE_GROUP_NAME string = resourceGroupName
output HOST_NAME string = webSiteModule.outputs.hostName
