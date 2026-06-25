// ----------------------------------------------------------------------------------------------------
// Shared Pipeline Parameter File (Azure DevOps + GitHub Actions)
// ----------------------------------------------------------------------------------------------------
using './main.bicep'

param appName = '#{APP_NAME}#'
param environmentCode = '#{ENVCODE}#'
param location = '#{RESOURCE_GROUP_LOCATION}#'
param instanceNumber = '#{INSTANCE_NUMBER}#'
param deploymentType = '#{DEPLOYMENT_TYPE}#'
param appDataSource = 'SQL'

param adminUserList = '#{ADMIN_USER_LIST}#'
param adInstance = '#{LOGIN_INSTANCEENDPOINT}#'
param adDomain = '#{LOGIN_DOMAIN}#'
param adTenantId = '#{LOGIN_TENANTID}#'
param adClientId = '#{LOGIN_CLIENTID}#'
param webApiKey = '#{WEB_API_KEY}#'
param servicePlanName = '#{EXISTING_SERVICEPLAN_NAME}#'
param servicePlanResourceGroupName = '#{EXISTING_SERVICEPLAN_RESOURCE_GROUP_NAME}#'
param webAppKind = 'linux' // 'linux' or 'windows'

param sqlAdminLoginUserId = '#{SQLADMIN_LOGIN_USERID}#'
param sqlAdminLoginUserSid = '#{SQLADMIN_LOGIN_USERSID}#'
param sqlAdminLoginTenantId = '#{SQLADMIN_LOGIN_TENANTID}#'

param sqlDatabaseName = '#{SQL_DATABASE_NAME}#'
param existingSqlServerName = '#{EXISTING_SQLSERVER_NAME}#'
param existingSqlDatabaseName = '#{EXISTING_SQLDATABASE_NAME}#'
param existingSqlServerResourceGroupName = '#{EXISTING_SQLSERVER_RESOURCE_GROUP_NAME}#'

param adminUserId = '#{KEYVAULT_OWNER_USERID}#'

param azureMapsSubscriptionKey = '#{AZUREMAPS_SUBSCRIPTIONKEY}#'
param azureMapsClientId = '#{AZUREMAPS_CLIENTID}#'

param azureAIFoundryEndpoint = '#{AZUREAIFOUNDRY_ENDPOINT}#'
param azureAIFoundryApiKey = '#{AZUREAIFOUNDRY_APIKEY}#'
param azureAIFoundryDeploymentName = '#{AZUREAIFOUNDRY_DEPLOYMENTNAME}#'
param azureAIFoundryProjectEndpoint = '#{AZUREAIFOUNDRY_PROJECTENDPOINT}#'
param azureAIFoundryAgentName = '#{AZUREAIFOUNDRY_AGENTNAME}#'
param azureAIFoundryAgentVersion = '#{AZUREAIFOUNDRY_AGENTVERSION}#'
