// --------------------------------------------------------------------------------
// Grant the application identity data-plane access to the Foundry resource group.
// --------------------------------------------------------------------------------
param identityPrincipalId string
param foundrySubscriptionId string

var cognitiveServicesOpenAIUserRoleId = '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd'

resource openAIUserRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, identityPrincipalId, cognitiveServicesOpenAIUserRoleId)
  properties: {
    principalId: identityPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId(
      foundrySubscriptionId,
      'Microsoft.Authorization/roleDefinitions',
      cognitiveServicesOpenAIUserRoleId
    )
    description: 'Allow the Travel Tracker managed identity to invoke Foundry OpenAI models.'
  }
}
