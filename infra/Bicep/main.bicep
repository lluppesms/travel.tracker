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
param instanceNumber string = '1'

@description('Deployment type for the web application')
param deploymentType string = 'webapp'  // ['webapp', 'containerapp', 'functionapp', 'all']

@description('Deploy only website infrastructure (skip SQL and Function resources).')
param websiteOnly bool = false

param servicePlanName string = ''
param servicePlanResourceGroupName string = '' // if using an existing service plan in a different resource group

param webAppKind string = 'linux' // 'linux' or 'windows'
param webSiteSku string = 'B1'
param webStorageSku string = 'Standard_LRS'
param webApiKey string = ''


param sqlDatabaseName string = 'traveltracker'
@allowed(['Basic','Standard','Premium','BusinessCritical','GeneralPurpose'])
param sqlSkuTier string = 'GeneralPurpose'
param sqlSkuFamily string = 'Gen5'
param sqlSkuName string = 'GP_S_Gen5'
param sqlAdminLoginUserId string = ''
param sqlAdminLoginUserSid string = ''
param sqlAdminLoginTenantId string = ''
param sqlAdminUser string = ''
@secure()
param sqlAdminPassword string = ''

param existingSqlServerName string = ''
param existingSqlDatabaseName string = ''
param existingSqlServerResourceGroupName string = ''
param existingLogAnalyticsWorkspaceName string = ''
param existingLogAnalyticsWorkspaceResourceGroupName string = ''

param adInstance string = environment().authentication.loginEndpoint // 'https://login.microsoftonline.com/'
param adDomain string = ''
param adTenantId string = ''
param adClientId string = ''
param adCallbackPath string = '/signin-oidc'

param adminUserList string = ''

@description('Data source used by the web app. JSON avoids database-backed repository usage.')
@allowed(['JSON', 'SQL'])
param appDataSource string = 'JSON'
param appSwaggerEnabled string = 'true'

@description('AI service provider used by the web application. "CopilotSDK" or "AgentFramework"')
param aiServiceProvider string = ''

param azureOpenAIChatEndpoint string = ''
param azureOpenAIChatDeploymentName string = ''
param azureOpenAIChatApiKey string = ''
param azureOpenAIChatMaxTokens string = ''
param azureOpenAIChatTemperature string = ''
param azureOpenAIChatTopP string = ''
param azureOpenAIImageEndpoint string = ''
param azureOpenAIImageDeploymentName string = ''
param azureOpenAIImageApiKey string = ''

@secure()
param azureMapsSubscriptionKey string = ''
param azureMapsClientId string = ''

param azureAIFoundryEndpoint string = ''
@secure()
param azureAIFoundryApiKey string = ''
param azureAIFoundryDeploymentName string = ''
param azureAIFoundryProjectEndpoint string = ''
param azureAIFoundryAgentName string = ''
param azureAIFoundryAgentVersion string = ''


@description('Add Role Assignments for the user assigned identity?')
param addRoleAssignments bool = true

@description('Create a separate user-assigned managed identity. When false, each resource uses its own system-assigned identity.')
param createUserAssignedIdentity bool = false

@description('Add this Admin User Id to KeyVault Access')
param adminUserId string = ''

// calculated variables disguised as parameters
param runDateTime string = utcNow()

// --------------------------------------------------------------------------------
var deploymentSuffix = '-${runDateTime}'
var existingServicePlanNameEffective = empty(trim(servicePlanName)) || contains(servicePlanName, '#{') ? '' : trim(servicePlanName)
var existingServicePlanRgNameEffective = empty(trim(servicePlanResourceGroupName)) || contains(servicePlanResourceGroupName, '#{') ? '' : trim(servicePlanResourceGroupName)
var existingSqlServerNameEffective = empty(trim(existingSqlServerName)) || contains(existingSqlServerName, '#{') ? '' : trim(existingSqlServerName)
var existingSqlDatabaseNameEffective = empty(trim(existingSqlDatabaseName)) || contains(existingSqlDatabaseName, '#{') ? '' : trim(existingSqlDatabaseName)
var existingSqlServerRgNameEffective = empty(trim(existingSqlServerResourceGroupName)) || contains(existingSqlServerResourceGroupName, '#{') ? '' : trim(existingSqlServerResourceGroupName)
var existingLogAnalyticsWorkspaceNameEffective = empty(trim(existingLogAnalyticsWorkspaceName)) || contains(existingLogAnalyticsWorkspaceName, '#{') ? '' : trim(existingLogAnalyticsWorkspaceName)
var existingLogAnalyticsWorkspaceRgNameEffective = empty(trim(existingLogAnalyticsWorkspaceResourceGroupName)) || contains(existingLogAnalyticsWorkspaceResourceGroupName, '#{') ? '' : trim(existingLogAnalyticsWorkspaceResourceGroupName)
var effectiveManagedIdentityId = createUserAssignedIdentity ? identity!.outputs.managedIdentityId : ''
var effectiveManagedIdentityPrincipalId = createUserAssignedIdentity ? identity!.outputs.managedIdentityPrincipalId : ''
var commonTags = {
  LastDeployed: runDateTime
  Application: appName
  Environment: environmentCode
}
var resourceGroupName = resourceGroup().name
var useSqlDataSource = toUpper(appDataSource) == 'SQL' && !websiteOnly
var webAppConnectionString = useSqlDataSource ? sqlDbModule!.outputs.identityConnectionString : ''
var deploymentTypeNormalized = toLower(deploymentType)
var deployWebAppEffective = contains(['webapp', 'all'], deploymentTypeNormalized)
var deployWebsiteEffective = deployWebAppEffective
var keyVaultApplicationUserObjectIds = deployWebsiteEffective
  ? concat(
      deployWebAppEffective ? [ webSiteModule!.outputs.userManagedPrincipalId, webSiteModule!.outputs.systemPrincipalId ] : [])
  : [ identity.outputs.managedIdentityPrincipalId ]
// var resourceToken = toLower(uniqueString(resourceGroup().id, location))

// --------------------------------------------------------------------------------
module resourceNames 'resourcenames.bicep' = {
  name: 'resourcenames${deploymentSuffix}'
  params: {
    appName: appName
    environmentCode: environmentCode
    instanceNumber: instanceNumber
  }
}

// --------------------------------------------------------------------------------
module logAnalyticsWorkspaceModule './modules/monitor/loganalyticsworkspace.bicep' = {
  name: 'logAnalytics${deploymentSuffix}'
  params: {
    logAnalyticsWorkspaceName: resourceNames.outputs.logAnalyticsWorkspaceName
    existingLogAnalyticsWorkspaceName: existingLogAnalyticsWorkspaceNameEffective
    existingLogAnalyticsWorkspaceResourceGroupName: existingLogAnalyticsWorkspaceRgNameEffective
    location: location
    commonTags: commonTags
  }
}

// --------------------------------------------------------------------------------
module storageModule './modules/storage/storageaccount.bicep' = {
  name: 'storage${deploymentSuffix}'
  params: {
    storageSku: webStorageSku
    storageAccountName: resourceNames.outputs.storageAccountName
    location: location
    commonTags: commonTags
    containerNames: ['input', 'output', 'backup-data']
  }
}

// --------------------------------------------------------------------------------
module sqlDbModule './modules/database/sqlserver.bicep' = {
  name: 'sql-server${deploymentSuffix}'
  params: {
    sqlServerName: resourceNames.outputs.sqlServerName
    sqlDBName: sqlDatabaseName
    existingSqlServerName: existingSqlServerNameEffective
    existingSqlDatabaseName: existingSqlDatabaseNameEffective
    existingSqlServerResourceGroupName: existingSqlServerRgNameEffective
    sqlSkuTier: sqlSkuTier
    sqlSkuName: sqlSkuName
    sqlSkuFamily: sqlSkuFamily
    mincores: 1
    autopause: 60
    location: location
    commonTags: commonTags
    adAdminUserId: sqlAdminLoginUserId
    adAdminUserSid: sqlAdminLoginUserSid
    adAdminTenantId: sqlAdminLoginTenantId
    userAssignedIdentityResourceId: effectiveManagedIdentityId
    sqlAdminUser:sqlAdminUser
    sqlAdminPassword: sqlAdminPassword
    workspaceId: logAnalyticsWorkspaceModule.outputs.id
    addSecurityControlIgnoreTag: true
  }
}

// --------------------------------------------------------------------------------
module identity './modules/iam/identity.bicep' = if (createUserAssignedIdentity) {
  name: 'appIdentity${deploymentSuffix}'
  params: {
    identityName: resourceNames.outputs.userAssignedIdentityName
    location: location
  }
}

module appRoleAssignments './modules/iam/roleassignments.bicep' = if (addRoleAssignments && createUserAssignedIdentity) {
  name: 'appRoleAssignments${deploymentSuffix}'
  params: {
    identityPrincipalId: identity!.outputs.managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
    storageAccountName: storageModule.outputs.name
    keyVaultName:  keyVaultModule.outputs.name
  }
}
// also add rights to the web app storage account (App Service only)
module appRoleAssignments2 './modules/iam/roleassignments.bicep' = if (addRoleAssignments && deployWebAppEffective) {
  name: 'appRoleAssignments-webapp-storage${deploymentSuffix}'
  params: {
    identityPrincipalId: webSiteModule!.outputs.systemPrincipalId
    principalType: 'ServicePrincipal'
    storageAccountName: storageModule.outputs.name
  }
}

// --------------------------------------------------------------------------------
module keyVaultModule './modules/security/keyvault.bicep' = {
  name: 'keyVault${deploymentSuffix}'
  params: {
    keyVaultName: resourceNames.outputs.keyVaultName
    location: location
    commonTags: commonTags
    keyVaultOwnerUserId: adminUserId
    adminUserObjectIds: createUserAssignedIdentity ? [ identity!.outputs.managedIdentityPrincipalId ] : []
    applicationUserObjectIds: keyVaultApplicationUserObjectIds
    workspaceId: logAnalyticsWorkspaceModule.outputs.id
    publicNetworkAccess: 'Enabled'
    allowNetworkAccess: 'Allow'
    useRBAC: true
  }
}

module keyVaultStorageSecret './modules/security/keyvaultsecretstorageconnection.bicep' = if (deployWebAppEffective) {
  name: 'keyVaultStorageSecret${deploymentSuffix}'
  params: {
    keyVaultName: keyVaultModule.outputs.name
    secretName: 'azurefilesconnectionstring'
    storageAccountName: storageModule!.outputs.name
  }
}

// --------------------------------------------------------------------------------
// App Service Infrastructure (deployed when deploymentType is webapp/appservice alias or all)
// --------------------------------------------------------------------------------
module appServicePlanModule './modules/webapp/websiteserviceplan.bicep' = if (deployWebAppEffective) {
  name: 'appService${deploymentSuffix}'
  params: {
    location: location
    commonTags: commonTags
    sku: webSiteSku
    appServicePlanName: empty(existingServicePlanNameEffective) ? resourceNames.outputs.webSiteAppServicePlanName : existingServicePlanNameEffective
    existingServicePlanName: existingServicePlanNameEffective
    existingServicePlanResourceGroupName: existingServicePlanRgNameEffective
    webAppKind: webAppKind
  }
}

module webSiteModule './modules/webapp/website.bicep' = if (deployWebAppEffective) {
  name: 'webSite${deploymentSuffix}'
  params: {
    webSiteName: resourceNames.outputs.webSiteName
    location: location
    appInsightsLocation: location
    commonTags: commonTags
    environmentCode: environmentCode
    webAppKind: webAppKind
    managedIdentityId: effectiveManagedIdentityId
    managedIdentityPrincipalId: effectiveManagedIdentityPrincipalId
    workspaceId: logAnalyticsWorkspaceModule.outputs.id
    appServicePlanName: appServicePlanModule!.outputs.name
    appServicePlanResourceGroupName: appServicePlanModule!.outputs.resourceGroupName
// In a Linux app service, any nested JSON app key like AppSettings:MyKey needs to be 
// configured in App Service as AppSettings__MyKey for the key name. 
// In other words, any : should be replaced by __ (double underscore).
// NOTE: See https://learn.microsoft.com/en-us/azure/app-service/configure-common?tabs=portal
   customAppSettings: {
      AppSettings__AppInsights_InstrumentationKey: '' // Will be set by base settings
      AppSettings__DefaultConnection: webAppConnectionString
      SqlServer__ConnectionString: webAppConnectionString
      AppSettings__EnvironmentName: environmentCode
      AppSettings__EnableSwagger: appSwaggerEnabled
      AppSettings__DataSource: appDataSource
      AppSettings__ApiKey: webApiKey
      AppSettings__AdminUserList: adminUserList
      AppSettings__AiServiceProvider: aiServiceProvider
      AppSettings__AzureOpenAI__Chat__Endpoint: azureOpenAIChatEndpoint
      AppSettings__AzureOpenAI__Chat__DeploymentName: azureOpenAIChatDeploymentName
      AppSettings__AzureOpenAI__Chat__ApiKey: azureOpenAIChatApiKey
      AppSettings__AzureOpenAI__Chat__MaxTokens: azureOpenAIChatMaxTokens
      AppSettings__AzureOpenAI__Chat__Temperature: azureOpenAIChatTemperature
      AppSettings__AzureOpenAI__Chat__TopP: azureOpenAIChatTopP
      AppSettings__AzureOpenAI__Image__Endpoint: azureOpenAIImageEndpoint
      AppSettings__AzureOpenAI__Image__DeploymentName: azureOpenAIImageDeploymentName
      AppSettings__AzureOpenAI__Image__ApiKey: azureOpenAIImageApiKey
      AppSettings__BlobStorageAccountName: storageModule.outputs.name
      AzureAD__Instance: adInstance
      AzureAD__Domain: adDomain
      AzureAD__TenantId: adTenantId
      AzureAD__ClientId: adClientId
      AzureAD__CallbackPath: adCallbackPath

      AzureMaps__SubscriptionKey: azureMapsSubscriptionKey
      AzureMaps__ClientId: azureMapsClientId

      AzureAIFoundry__Endpoint: azureAIFoundryEndpoint
      AzureAIFoundry__ApiKey: azureAIFoundryApiKey
      AzureAIFoundry__DeploymentName: azureAIFoundryDeploymentName
      AzureAIFoundry__ProjectEndpoint: azureAIFoundryProjectEndpoint
      AzureAIFoundry__AgentName: azureAIFoundryAgentName
      AzureAIFoundry__AgentVersion: azureAIFoundryAgentVersion
    }
  }
}

// --------------------------------------------------------------------------------
output SUBSCRIPTION_ID string = subscription().subscriptionId
output RESOURCE_GROUP_NAME string = resourceGroupName
output DEPLOYMENT_TYPE string = deploymentTypeNormalized
output WEB_HOST_NAME string = deployWebAppEffective ? webSiteModule!.outputs.hostName : ''
output WEB_URL string = deployWebAppEffective ? 'https://${webSiteModule!.outputs.hostName}' : ''
