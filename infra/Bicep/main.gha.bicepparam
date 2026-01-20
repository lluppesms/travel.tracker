// ----------------------------------------------------------------------------------------------------
// Bicep Parameter File
// ----------------------------------------------------------------------------------------------------
using './main.bicep'

param appName = '#{APP_NAME}#'
param environmentCode = '#{envCode}#'
param location = '#{RESOURCE_GROUP_LOCATION}#'
param instanceNumber = '#{INSTANCE_NUMBER}#'

param apiKey = '#{API_KEY}#'

param adInstance = '#{LOGIN_INSTANCEENDPOINT}#'
param adDomain = '#{LOGIN_DOMAIN}#'
param adTenantId = '#{LOGIN_TENANTID}#'
param adClientId = '#{LOGIN_CLIENTID}#'
param adClientSecret = '#{LOGIN_CLIENTSECRET}#'

param servicePlanName = '#{EXISTING_SERVICEPLAN_NAME}#'
param servicePlanResourceGroupName = '#{EXISTING_SERVICEPLAN_RESOURCE_GROUP_NAME}#'

param existingSqlServerName = '#{EXISTING_SQLSERVER_NAME}#'
param existingSqlServerResourceGroupName = '#{EXISTING_SQLSERVER_RESOURCE_GROUP_NAME}#'

param sqlAdminLoginUserId = '#{SQLADMIN_LOGIN_USERID}#'
param sqlAdminLoginUserSid = '#{SQLADMIN_LOGIN_USERSID}#'
param sqlAdminLoginTenantId = '#{SQLADMIN_LOGIN_TENANTID}#'

param azureMapsSubscriptionKey = '#{AZUREMAPS_SUBSCRIPTIONKEY}#'
param azureMapsClientId = '#{AZUREMAPS_CLIENTID}#'

param azureAIFoundryEndpoint = '#{AZUREAIFOUNDRY_ENDPOINT}#'
param azureAIFoundryApiKey = '#{AZUREAIFOUNDRY_APIKEY}#'
param azureAIFoundryDeploymentName = '#{AZUREAIFOUNDRY_DEPLOYMENTNAME}#'
