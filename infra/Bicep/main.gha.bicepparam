// ----------------------------------------------------------------------------------------------------
// Bicep Parameter File
// ----------------------------------------------------------------------------------------------------

using './main.bicep'

param appName = '#{APP_NAME}#'
param environmentCode = '#{envCode}#'

param location = '#{RESOURCEGROUP_LOCATION}#'
//param servicePlanName = '#{servicePlanName}#'

param apiKey = '#{API_KEY}#'

param adInstance = '#{LOGIN_INSTANCEENDPOINT}#'
param adDomain = '#{LOGIN_DOMAIN}#'
param adTenantId = '#{LOGIN_TENANTID}#'
param adClientId = '#{LOGIN_CLIENTID}#'
param adClientSecret = '#{LOGIN_CLIENTSECRET}#'

param sqlADAdminLoginUserId = '#{AD_SQL_ADMIN_USERID}#'
param sqlADAdminLoginUserSid = '#{AD_SQL_ADMIN_SID}#'
param sqlADAdminLoginTenantId = '#{AD_SQL_ADMIN_TENANTID}#'

param azureMapsSubscriptionKey = '#{AZUREMAPS_SUBSCRIPTIONKEY}#'
param azureMapsClientId = '#{AZUREMAPS_CLIENTID}#'

param azureAIFoundryEndpoint = '#{AZUREAIFOUNDRY_ENDPOINT}#'
param azureAIFoundryApiKey = '#{AZUREAIFOUNDRY_APIKEY}#'
param azureAIFoundryDeploymentName = '#{AZUREAIFOUNDRY_DEPLOYMENTNAME}#'
