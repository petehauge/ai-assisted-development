# Azure environment

| | |
|---|---|
| Subscription | PETEHAUGE MSDN Subscription (`4c36dcce-4d41-41d6-af6d-d12040771ba8`) |
| Tenant | `5bbe7613-68e7-4cdb-93ba-03768614bf74` |
| Resource group | `ai-assisted-staging-rg` |
| Regions | `westus3` (Foundry, web app, SQL), `westus2` (everything else) |
| Generated | 2026-11-10 (UTC), by `/azure-secrets` |

This file lists what exists and where to find it. It contains no secret values; those live only in the gitignored `secrets.json` at the repo root, referenced below by key path (for example `Foundry:ProjectEndpoint`).

## Defaults the application reads

| Key path | Value | Notes |
|---|---|---|
| `Foundry:ProjectEndpoint` | `https://aiasst-foundry-m4gvse.services.ai.azure.com/api/projects/aiasst-project` | The only Foundry project |
| `Foundry:DefaultDeployment` | `gpt-5-mini` | Two chat deployments exist; chosen to match the hosted apps' `Foundry__DefaultDeployment` setting (pending confirmation) |
| `Foundry:EmbeddingDeployment` | `text-embedding-3-small` | The only embedding deployment |

## Microsoft Foundry

| Resource | Type | Region | Endpoint | Key paths |
|---|---|---|---|---|
| `aiasst-foundry-m4gvse` | `Microsoft.CognitiveServices/accounts` (AIServices) | westus3 | `https://aiasst-foundry-m4gvse.cognitiveservices.azure.com/` | `Foundry:ResourceName`, `Foundry:ResourceId`, `Foundry:Endpoint`, `Foundry:OpenAIEndpoint`, `Foundry:Endpoints`, `Foundry:ApiKey`, `Foundry:Deployments` |

OpenAI-compatible endpoint: `https://aiasst-foundry-m4gvse.openai.azure.com/` (`Foundry:OpenAIEndpoint`). `Foundry:ApiKey` works only on the OpenAI-compatible endpoint; the app uses Entra ID per `CLAUDE.md`.

### Foundry projects

| Project | Project endpoint | Key paths |
|---|---|---|
| `aiasst-project` (default) | `https://aiasst-foundry-m4gvse.services.ai.azure.com/api/projects/aiasst-project` | `Foundry:ProjectEndpoint`, `Foundry:Projects:aiasst-project:Endpoint` |

### Model deployments

| Deployment | Model | Version | SKU | Capacity | Capabilities |
|---|---|---|---|---|---|
| `gpt-5-mini` (default) | gpt-5-mini | 2025-08-07 | GlobalStandard | 50 | chat completion, responses, agents |
| `gpt-5.4-mini` | gpt-5.4-mini | 2026-03-17 | GlobalStandard | 50 | chat completion, responses, agents |
| `text-embedding-3-small` | text-embedding-3-small | 1 | GlobalStandard | 50 | embeddings |

### Foundry access

- Authentication: Microsoft Entra ID (token credential such as `DefaultAzureCredential`); locally the developer's `az login`, hosted the app's managed identity.
- Signed-in user: `petehauge@outlook.com`.
- Role check on project `aiasst-project` (read-only `az role assignment list`, including inherited and group assignments): **Foundry User** on the Foundry account, plus **Owner** and **Foundry User** at subscription scope. The listing can miss some assignments (for example unactivated PIM-eligible roles), so treat it as a hint, not proof.
- Not checked: role assignments for the web and function app managed identities (both apps have a system-assigned identity).

## App hosting

| Resource | Type | Region | URL | Key paths |
|---|---|---|---|---|
| `aiasst-web-m4gvse` | Web app (`app,linux`, `DOTNETCORE\|10.0`) | westus3 | `https://aiasst-web-m4gvse.azurewebsites.net` | `AppServices:aiasst-web-m4gvse:*` |
| `aiasst-func-m4gvse` | Function app (`functionapp,linux`, dotnet-isolated 10) | westus2 | `https://aiasst-func-m4gvse.azurewebsites.net` | `FunctionApps:aiasst-func-m4gvse:*` |
| `aiasst-swa-m4gvse` | Static web app | westus2 | `https://happy-dune-0c361941e.5.azurestaticapps.net` | `Other:aiasst-swa-m4gvse` |
| `aiasst-web-plan-m4gvse` | App Service plan (linux) | westus3 | | `Other:aiasst-web-plan-m4gvse` |
| `aiasst-func-plan-m4gvse` | App Service plan (function app) | westus2 | | `Other:aiasst-func-plan-m4gvse` |

Both apps have a system-assigned managed identity and no connection strings configured.

### App setting names

Web app `aiasst-web-m4gvse` (`AppServices:aiasst-web-m4gvse:AppSettings`): `APPLICATIONINSIGHTS_CONNECTION_STRING`, `Foundry__ProjectEndpoint`, `Foundry__Endpoint`, `Foundry__DefaultDeployment`, `Foundry__EmbeddingDeployment`, `Storage__BlobEndpoint`, `Storage__QueueEndpoint`, `Storage__TableEndpoint`, `Cosmos__Endpoint`, `Cosmos__Database`, `Cosmos__Container`, `KeyVault__Uri`, `Search__Endpoint`, `ServiceBus__FullyQualifiedNamespace`, `ServiceBus__Queue`, `Sql__Database`, `Sql__ConnectionString`, `Sql__Server`.

Function app `aiasst-func-m4gvse` (`FunctionApps:aiasst-func-m4gvse:AppSettings`): the same names as the web app, plus `AzureWebJobsStorage__credential`, `AzureWebJobsStorage__blobServiceUri`, `AzureWebJobsStorage__queueServiceUri`, `AzureWebJobsStorage__tableServiceUri`, `ServiceBusConnection__fullyQualifiedNamespace`, `CosmosConnection__accountEndpoint`.

The settings point to identity-based connections (endpoints and namespaces rather than keys); the SQL connection string uses `Authentication=Active Directory Default`. Non-secret names from the settings: Cosmos container `items`, Service Bus queue `work-items`, SQL database `appdb`.

## Data and messaging

| Resource | Type | Region | Endpoint | Key paths |
|---|---|---|---|---|
| `aiasst-cosmos-m4gvse` | Cosmos DB (NoSQL) | westus2 | `https://aiasst-cosmos-m4gvse.documents.azure.com:443/` | `Cosmos:aiasst-cosmos-m4gvse:Endpoint`, `:ConnectionString`, `:Databases` (`appdb`) |
| `aiasst-sql-w3-m4gvse` | Azure SQL server (Entra-only auth) | westus3 | `aiasst-sql-w3-m4gvse.database.windows.net` | `Sql:aiasst-sql-w3-m4gvse:Fqdn`, `:Databases` (`appdb`, serverless, free limit) |
| `aiasststm4gvse` | Storage account (StorageV2) | westus2 | `https://aiasststm4gvse.blob.core.windows.net/` | `Storage:aiasststm4gvse:BlobEndpoint`, `:QueueEndpoint`, `:TableEndpoint`, `:FileEndpoint`, `:ConnectionString` |
| `aiasst-sb-m4gvse` | Service Bus namespace | westus2 | `https://aiasst-sb-m4gvse.servicebus.windows.net:443/` | `ServiceBus:aiasst-sb-m4gvse:Endpoint`, `:ConnectionString` |
| `aiasst-search-m4gvse` | Azure AI Search | westus2 | `https://aiasst-search-m4gvse.search.windows.net` | `AISearch:aiasst-search-m4gvse:Endpoint`, `:AdminKey`, `:QueryKey` |
| `aiasst-kv-m4gvse` | Key Vault | westus2 | `https://aiasst-kv-m4gvse.vault.azure.net/` | `KeyVault:aiasst-kv-m4gvse:Uri`, `:Secrets` (vault currently has no secrets) |

## Monitoring

| Resource | Type | Region | Key paths |
|---|---|---|---|
| `aiasst-ai-m4gvse` | Application Insights | westus2 | `ApplicationInsights:aiasst-ai-m4gvse:ConnectionString` |
| `aiasst-logs-m4gvse` | Log Analytics workspace | westus2 | `Other:aiasst-logs-m4gvse` |
| `Failure Anomalies - aiasst-ai-m4gvse` | Smart detector alert rule | global | `Other:Failure Anomalies - aiasst-ai-m4gvse` |

## Gaps

- `aiasst-sql-w3-m4gvse`: SQL passwords cannot be read; the server uses Entra-only authentication, so connect with Entra ID (`Authentication=Active Directory Default`).
- Foundry: two chat-capable deployments (`gpt-5-mini`, `gpt-5.4-mini`); `Foundry:DefaultDeployment` is set to `gpt-5-mini` to match the hosted apps, pending user confirmation.
- Managed identity role assignments for the web and function apps were not checked.
