# Set up GitHub Secrets

The GitHub workflows in this project require several secrets set at the repository level.

---

## Azure Credentials

Before you begin, you will need to set up the Azure Credentials secrets in the GitHub Secrets at the Repository level (or the environment level).  These secrets and credentials will allow the GitHub Actions to deploy into Azure.

See the reference links below for more info on how to create the service principal and set up the Federated Credentials.

> Note: this service principal must have **Contributor** rights to your subscription (or resource group) to deploy the resources. If you want to assign roles in the Bicep, it will also need the **User Access Administrator** role.  (Alternatively, you can put the service principal in the **Owner** role also, but that doesn't follow least privilege.)

### Update on Federated Credentials
Prior to July 2026, when you set up a federated identity credential in an App Registration to use in a GH Action, you only needed to supply the owner name and repo name and (environment/branch). All repositories created after **July 15, 2026** will now have to also supply the **IMMUTABLE** values (which are numeric values for the org/user and the repository).

To find those values, run these commands and they will return the numeric **IMMUTABLE** values:
```bash
gh api user --jq .id
gh api repos/<yourOrg>/<yourRepo> --jq .id
```

> NOTE: the first command is for a *USER* (i.e. lluppesms), NOT for an *ORG*…  I'm not sure if there is a different command for that.

Once the credentials are set up, customize and run this command to create these secrets:

``` bash
gh auth login

gh secret set --env dev AZURE_TENANT_ID -b <GUID>
gh secret set --env dev AZURE_CLIENT_ID -b <GUID>
gh secret set --env dev AZURE_SUBSCRIPTION_ID -b <yourAzureSubscriptionId>
```

---

## Bicep Configuration Values

These variables and secrets are used by the Bicep templates to configure the resource names that are deployed. The token-replacement step (`Replace Tokens` in [template-bicep-deploy.yml](workflows/template-bicep-deploy.yml)) substitutes every `#{TOKEN_NAME}#` placeholder in [main.bicepparam](../infra/Bicep/main.bicepparam) with a matching GitHub Actions `env`, `vars`, or `secrets` value of the same name — so every token below must exist as **either** a repository/environment **secret** or **variable** with that *exact* name. Make sure `APP_NAME` is unique to your deploy; it is used as the basis for the website name and all other globally-unique Azure resource names.

> Tip: values that are sensitive (credentials, connection strings, API keys) should be GitHub **Secrets** (`gh secret set`). Everything else (names, flags, numeric limits) can be a GitHub **Variable** (`gh variable set`) so they're visible in the Actions UI for troubleshooting.

To create these additional secrets and variables, customize and run this command:

### Core app / environment

``` bash
gh auth login

gh variable set APP_NAME -b 'xxx-traveltracker'
gh variable set ENVCODE -b 'dev'
gh variable set RESOURCE_GROUP_LOCATION -b 'centralus'
gh variable set RESOURCE_GROUP_PREFIX -b 'rg_traveltracker'
gh variable set INSTANCE_NUMBER -b 1
gh variable set DEPLOYMENT_TYPE -b 'webapp'
gh variable set ADD_ROLE_ASSIGNMENTS -b 'true'
gh variable set CREATE_USER_ASSIGNED_IDENTITY -b 'false'
gh variable set WEB_APP_ALWAYS_ON -b 'true'
gh secret set WEB_API_KEY -b 'somesecretstring'

gh variable set ADMIN_USER_LIST -b 'user1@domain.com,user2@domain.com'
gh secret set KEYVAULT_OWNER_USERID -b '<yourObjectId>'
```

> `ENVCODE` is normally supplied by the workflow's `inputs.envCode` (the environment picker), but if you run `main.bicepparam` manually or via `az deployment` you must also set it explicitly. Set `ADD_ROLE_ASSIGNMENTS` to `'true'` so the identity gets RBAC on other resources (SQL, Storage, Foundry); set `CREATE_USER_ASSIGNED_IDENTITY` to `'true'` to provision a separate user-assigned managed identity, or leave it `'false'` (default) to use each resource's own system-assigned identity — **note that the Foundry role assignment (below) is only created when `CREATE_USER_ASSIGNED_IDENTITY` is `'true'`.**

### Entra ID (Sign-in / Auth)

``` bash
gh secret set LOGIN_CLIENTID -b '<yourADClientId>'
gh secret set LOGIN_DOMAIN -b '<yourdomain>.onmicrosoft.com'
gh secret set LOGIN_INSTANCEENDPOINT -b 'https://login.microsoftonline.com/'
gh secret set LOGIN_TENANTID -b '<yourTenantId>'
```

### App Service Plan (optional reuse)

``` bash
gh variable set EXISTING_SERVICEPLAN_NAME -b ''
gh variable set EXISTING_SERVICEPLAN_RESOURCE_GROUP_NAME -b ''
```

### SQL Database

``` bash
gh variable set SQL_SERVER_NAME_PREFIX -b 'your-travel-tracker-server'
gh variable set SQL_DATABASE_NAME -b 'TravelTrackerDB'
gh variable set SQLADMIN_LOGIN_USERID -b 'youruser@yourdomain.com'
gh variable set SQLADMIN_LOGIN_USERSID -b 'yoursid'
gh variable set SQLADMIN_LOGIN_TENANTID -b 'yourtennant'

gh variable set EXISTING_SQLSERVER_NAME -b ''
gh variable set EXISTING_SQLDATABASE_NAME -b ''
gh variable set EXISTING_SQLSERVER_RESOURCE_GROUP_NAME -b ''
```

### Log Analytics (optional reuse)

``` bash
gh variable set EXISTING_LOG_ANALYTICS_WORKSPACE -b ''
gh variable set EXISTING_LOG_ANALYTICS_WORKSPACE_RESOURCE_GROUP_NAME -b ''
```

Leave the `EXISTING_*` variables blank to let Bicep create new resources. To reuse SQL resources, set both `EXISTING_SQLSERVER_NAME` and `EXISTING_SQLDATABASE_NAME`. To reuse an existing Log Analytics Workspace, set `EXISTING_LOG_ANALYTICS_WORKSPACE` to its name and optionally `EXISTING_LOG_ANALYTICS_WORKSPACE_RESOURCE_GROUP_NAME` if it is in a different resource group.

> `sqlAdminUser`/`sqlAdminPassword` are declared in `main.bicep` but are **not** tokenized in `main.bicepparam` and default to empty. Leaving them empty deploys the SQL Server with **Entra ID-only authentication** (no SQL secret needed). Only pass `--parameters sqlAdminUser=... sqlAdminPassword=...` on the `az deployment` command line if you specifically need SQL native auth enabled.

### Travel Assistant (Copilot SDK) — Phase 6

These configure the Travel Assistant feature (`aiServiceProvider`, write mode, Foundry model/endpoint, and runtime limits) added to `main.bicep`/`main.bicepparam` in Phase 6. `travelAssistantWriteMode` is **not** tokenized/a secret — it is hardcoded to `'Confirm'` in `main.bicepparam` because Bicep restricts it with `@allowed(['Confirm'])`.

``` bash
gh variable set AI_SERVICE_PROVIDER -b 'CopilotSDK'
gh variable set TRAVEL_ASSISTANT_MODEL_DEPLOYMENT_NAME -b 'gpt-5-mini'
gh variable set TRAVEL_ASSISTANT_FOUNDRY_ENDPOINT -b 'https://<your-foundry-resource>.services.ai.azure.com'
gh variable set TRAVEL_ASSISTANT_TOKEN_SCOPE -b 'https://ai.azure.com/.default'
gh variable set TRAVEL_ASSISTANT_COPILOT_HOME -b '/tmp/traveltracker-copilot'
gh variable set TRAVEL_ASSISTANT_TIME_ZONE_ID -b 'America/Chicago'
gh variable set TRAVEL_ASSISTANT_DATA_PROTECTION_KEYS_PATH -b ''
```

### Foundry Cross-Resource-Group RBAC — Phase 6

Set these so the deployment can grant the app's identity the **Cognitive Services OpenAI User** role on the resource group that hosts your Azure AI Foundry resource, even if it lives in a different resource group (or subscription) than the web app. Leave `FOUNDRY_RESOURCE_GROUP_NAME` blank to skip this role assignment.

``` bash
gh variable set FOUNDRY_RESOURCE_GROUP_NAME -b 'rg-foundry-shared'
gh variable set FOUNDRY_SUBSCRIPTION_ID -b '<foundrySubscriptionGuid>'
```

> Requires `CREATE_USER_ASSIGNED_IDENTITY=true` and `ADD_ROLE_ASSIGNMENTS=true` (see above), and the deploying service principal needs **User Access Administrator** on the Foundry resource group (or subscription) to create the role assignment there.

### Azure OpenAI (Chat & Image)

``` bash
gh secret set OPENAI_CHAT_ENDPOINT -b 'https://<your-openai-resource>.openai.azure.com/'
gh variable set OPENAI_CHAT_DEPLOYMENTNAME -b 'gpt-5-mini'
gh secret set OPENAI_CHAT_APIKEY -b '<yourOpenAIChatApiKey>'
gh variable set OPENAI_CHAT_MAXTOKENS -b '300'
gh variable set OPENAI_CHAT_TEMPERATURE -b '0.7'
gh variable set OPENAI_CHAT_TOPP -b '0.95'

gh secret set OPENAI_IMAGE_ENDPOINT -b 'https://<your-openai-resource>.openai.azure.com/'
gh variable set OPENAI_IMAGE_DEPLOYMENTNAME -b 'gpt-image-1.5'
gh secret set OPENAI_IMAGE_APIKEY -b '<yourOpenAIImageApiKey>'
```

### Azure Maps

``` bash
gh secret set AZUREMAPS_SUBSCRIPTIONKEY -b '<yourAzureMapsKey>'
gh variable set AZUREMAPS_CLIENTID -b '<yourAzureMapsClientId>'
```

### Azure AI Foundry (Agent)

``` bash
gh variable set AZUREAIFOUNDRY_ENDPOINT -b 'https://<your-foundry-resource>.services.ai.azure.com'
gh secret set AZUREAIFOUNDRY_APIKEY -b '<yourFoundryApiKey>'
gh variable set AZUREAIFOUNDRY_DEPLOYMENTNAME -b 'gpt-5-mini'
gh variable set AZUREAIFOUNDRY_PROJECTENDPOINT -b 'https://<your-foundry-resource>.services.ai.azure.com/api/projects/<projectName>'
gh variable set AZUREAIFOUNDRY_AGENTNAME -b '<yourAgentName>'
gh variable set AZUREAIFOUNDRY_AGENTVERSION -b '1'
```

---

## References

- [Deploying ARM Templates with GitHub Actions](https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/deploy-github-actions)
- [Manage Federated Identity Credential in Entra Id](https://learn.microsoft.com/en-us/entra/workload-id/workload-identity-federation-create-trust?pivots=identity-wif-apps-methods-azp) (MS Learn)
- [Immutable subject claims for GitHub Actions OIDC tokens](https://github.blog/changelog/2026-04-23-immutable-subject-claims-for-github-actions-oidc-tokens/) (GitHub Changelog Announcement - April 2026)
- [Migrate GitHub Actions federated credentials to immutable subjects](https://learn.microsoft.com/en-us/entra/workload-id/workload-identities-github-immutable-subjects) (MS Learn)
- [GitHub Secrets CLI](https://cli.github.com/manual/gh_secret_set)
- [GitHub Variables CLI](https://cli.github.com/manual/gh_variable_set)
