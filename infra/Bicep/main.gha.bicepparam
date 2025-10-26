// ----------------------------------------------------------------------------------------------------
// Bicep Parameter File
// ----------------------------------------------------------------------------------------------------

using './main.bicep'

param appName = '#{appName}#'
param environmentCode = '#{environmentNameLower}#'

param location = '#{location}#'
param servicePlanName = '#{servicePlanName}#'

param apiKey = '#{apiKey}#'

param adInstance = '#{adInstance}#'
param adDomain = '#{adDomain}#'
param adTenantId = '#{adTenantId}#'
param adClientId = '#{adClientId}#'
param adClientSecret = '#{adClientSecret}#'

param azureMapsSubscriptionKey = '#{AzureMaps_SubscriptionKey}#'
param azureMapsClientId = '#{AzureMaps_ClientId}#'

param azureAIFoundryEndpoint = '#{AzureAIFoundry_Endpoint}#'
param azureAIFoundryApiKey = '#{AzureAIFoundry_ApiKey}#'
param azureAIFoundryDeploymentName = '#{AzureAIFoundry_DeploymentName}#'
