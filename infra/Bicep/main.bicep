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
param adCallbackPath string = '/signin-oidc'

param appDataSource string = 'JSON'
param appSwaggerEnabled string = 'true'
param servicePlanName string = ''
param webAppKind string = 'linux' // 'linux' or 'windows'

param azureOpenAIChatEndpoint string = ''
param azureOpenAIChatDeploymentName string = ''
param azureOpenAIChatApiKey string = ''
param azureOpenAIChatMaxTokens string = ''
param azureOpenAIChatTemperature string = ''
param azureOpenAIChatTopP string = ''
param azureOpenAIImageEndpoint string = ''
param azureOpenAIImageDeploymentName string = ''
param azureOpenAIImageApiKey string = ''

param sqlServerNamePrefix string = ''
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
    sqlServerNamePrefix: sqlServerNamePrefix
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
module sqlDbModule 'sqlserver.bicep' = {
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
      AppSettings__ApiKey: apiKey
      AppSettings__AzureOpenAI__Chat__Endpoint: azureOpenAIChatEndpoint
      AppSettings__AzureOpenAI__Chat__DeploymentName: azureOpenAIChatDeploymentName
      AppSettings__AzureOpenAI__Chat__ApiKey: azureOpenAIChatApiKey
      AppSettings__AzureOpenAI__Chat__MaxTokens: azureOpenAIChatMaxTokens
      AppSettings__AzureOpenAI__Chat__Temperature: azureOpenAIChatTemperature
      AppSettings__AzureOpenAI__Chat__TopP: azureOpenAIChatTopP
      AppSettings__AzureOpenAI__Image__Endpoint: azureOpenAIImageEndpoint
      AppSettings__AzureOpenAI__Image__DeploymentName: azureOpenAIImageDeploymentName
      AppSettings__AzureOpenAI__Image__ApiKey: azureOpenAIImageApiKey
      AzureAD__Instance: adInstance
      AzureAD__Domain: adDomain
      AzureAD__TenantId: adTenantId
      AzureAD__ClientId: adClientId
      AzureAD__CallbackPath: adCallbackPath
    }
  }
}


output SUBSCRIPTION_ID string = subscription().subscriptionId
output RESOURCE_GROUP_NAME string = resourceGroupName
output HOST_NAME string = webSiteModule.outputs.hostName
