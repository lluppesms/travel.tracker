// --------------------------------------------------------------------------------
// Bicep file that builds all the resource names used by other Bicep templates
// --------------------------------------------------------------------------------
param appName string = ''
// @allowed(['azd','gha','azdo','dev','demo','qa','stg','ct','prod'])
param environmentCode string = 'azd'
param functionStorageNameSuffix string = 'func'
param dataStorageNameSuffix string = 'data'
param instanceNumber string = '1'

// --------------------------------------------------------------------------------
var sanitizedEnvironment = toLower(environmentCode)
var sanitizedAppNameWithDashes = replace(replace(toLower(appName), ' ', ''), '_', '')
// var sanitizedAppName = replace(replace(replace(toLower(appName), ' ', ''), '-', ''), '_', '')
var sanitizedAppInstanceNameWithDashes = replace(replace(toLower('${appName}${instanceNumber}'), ' ', ''), '_', '')
var sanitizedAppNameInstance = replace(replace(replace(toLower('${appName}${instanceNumber}'), ' ', ''), '_', ''), '-', '')

// pull resource abbreviations from a common JSON file
var resourceAbbreviations = loadJsonContent('./data/abbreviation.json')

// --------------------------------------------------------------------------------
var webSiteName = environmentCode == 'prod' ? toLower('${sanitizedAppNameWithDashes}') : toLower('${sanitizedAppInstanceNameWithDashes}-${sanitizedEnvironment}')
output webSiteName string                = webSiteName
output webSiteAppServicePlanName string  = '${webSiteName}-${resourceAbbreviations.webServerFarms}'
output webSiteAppInsightsName string     = '${webSiteName}-${resourceAbbreviations.webSitesAppService}'

output sqlServerName string              = toLower('${sanitizedAppNameInstance}-${resourceAbbreviations.sqlServers}-${sanitizedEnvironment}')
output cosmosDatabaseName string         = toLower('${sanitizedAppNameInstance}-${resourceAbbreviations.documentDBDatabaseAccounts}-${sanitizedEnvironment}')

output logAnalyticsWorkspaceName string  = toLower('${sanitizedAppInstanceNameWithDashes}-${sanitizedEnvironment}-${resourceAbbreviations.operationalInsightsWorkspaces}')
output userAssignedIdentityName string   = toLower('${sanitizedAppNameInstance}-${resourceAbbreviations.managedIdentityUserAssignedIdentities}-${sanitizedEnvironment}')

// Key Vaults and Storage Accounts can only be 24 characters long
output keyVaultName string               = take('${sanitizedAppNameInstance}${resourceAbbreviations.keyVaultVaults}${sanitizedEnvironment}', 24)
output storageAccountName string         = take('${sanitizedAppNameInstance}${resourceAbbreviations.storageStorageAccounts}${dataStorageNameSuffix}${sanitizedEnvironment}', 24)
output functionStorageName string        = take('${sanitizedAppNameInstance}${resourceAbbreviations.storageStorageAccounts}${functionStorageNameSuffix}${sanitizedEnvironment}', 24)
