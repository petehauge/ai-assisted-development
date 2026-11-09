---
name: azure-secrets
description: Pull resource details and secrets (endpoints, keys, model deployments, app settings, connection strings) from an Azure resource group into the gitignored secrets.json at the repo root, and write a secret-free inventory to docs/environment.md. Use when the user asks to read, collect, harvest, or pull secrets, keys, endpoints, or resource info from an Azure resource group or subscription.
argument-hint: "[resource-group]"
---

# Pull Azure resource details and secrets

Resource group: `$ARGUMENTS` (if empty, ask the user which resource group to use).

The output is two files:

- `secrets.json` (repo root, gitignored): every endpoint, key, connection string, and setting the app might need. Shape follows `secrets.example.json`.
- `docs/environment.md` (committed): the same inventory **without any secret values**, so planning and docs work can use it.

## Hard rules

- **Never print secret values.** Capture command output into variables or files, never echo keys, connection strings, or setting values to the console or chat. Summaries show key paths and masked values only (first 4 characters, then `…`).
- **Read-only.** Use only `list`, `show`, and `keys list` style commands. Never create, update, or delete Azure resources.
- **Stay in the repo.** Write only `secrets.json` and `docs/environment.md`. No temp files outside the repo; if you need scratch data, keep it in PowerShell variables.
- **Preserve hand-added values.** If `secrets.json` already exists, merge into it: keep existing sections and keys the scan does not produce (anything added by hand) and replace only the sections you refresh.

## Steps

1. **Confirm the target.** Run `az account show --query "{tenant:tenantId, subscription:name, subscriptionId:id, user:user.name}" -o json` and show the result. Ask the user to confirm this is the right tenant and subscription before reading any keys. If it is wrong, stop and let them switch (for example with `az account set --subscription <id>` or their tenant login skill).

2. **Inventory the resource group.** Run `az resource list -g <rg> -o json` and group resources by `type` (and `kind` where it matters).

3. **Collect details per resource type.** Use the table below. Run independent lookups in parallel where practical. If a command fails (missing role, unsupported SKU, extension needed), record a one-line entry in `_meta.gaps` and continue; never stop the whole scan for one resource.

| Resource type | Section in secrets.json | What to collect |
|---|---|---|
| `Microsoft.CognitiveServices/accounts` (kind `AIServices`, `OpenAI`, `CognitiveServices`) | `Foundry` (first AIServices/OpenAI account); additional accounts under `Foundry:Accounts:<name>` | `properties.endpoint` and `properties.endpoints` (`az cognitiveservices account show`), `key1` from `az cognitiveservices account keys list`, deployments from `az cognitiveservices account deployment list` (name, model name, model version, SKU) |
| `Microsoft.CognitiveServices/accounts/projects` (Foundry projects) | `Foundry:Projects:<name>` | project endpoint from `az resource show --ids <id>` `properties.endpoints` |
| `Microsoft.MachineLearningServices/workspaces` (hub-based Foundry) | `Foundry:Workspaces:<name>` | discovery URL and workspace id from `az resource show`; note in gaps if model keys live elsewhere |
| `Microsoft.Web/sites` (web apps) | `AppServices:<name>` | `https://<defaultHostName>`, `kind`, app settings (`az webapp config appsettings list`), connection strings (`az webapp config connection-string list`) |
| `Microsoft.Web/sites` with kind containing `functionapp` | `FunctionApps:<name>` | same as web apps, using `az functionapp config appsettings list` |
| `Microsoft.Storage/storageAccounts` | `Storage:<name>` | primary endpoints (`az storage account show`), connection string (`az storage account show-connection-string`) |
| `Microsoft.KeyVault/vaults` | `KeyVault:<name>` | `properties.vaultUri`; secret names (`az keyvault secret list`) and values (`az keyvault secret show --query value`) when data-plane access allows |
| `Microsoft.Search/searchServices` | `AISearch:<name>` | `https://<name>.search.windows.net`, admin key (`az search admin-key show`), query key (`az search query-key list`) |
| `Microsoft.DocumentDB/databaseAccounts` | `Cosmos:<name>` | `properties.documentEndpoint`, primary connection string (`az cosmosdb keys list --type connection-strings`) |
| `Microsoft.Insights/components` | `ApplicationInsights:<name>` | `properties.ConnectionString` (`az resource show` works without the extension) |
| `Microsoft.ServiceBus/namespaces` | `ServiceBus:<name>` | endpoint, `RootManageSharedAccessKey` primary connection string |
| `Microsoft.Cache/Redis` | `Redis:<name>` | host name, SSL port, primary key (`az redis list-keys`) |
| `Microsoft.Sql/servers` | `Sql:<name>` | FQDN and database names only (passwords cannot be read; note it in gaps) |
| anything else | `Other:<name>` | type, location, and resource id only |

4. **Write `secrets.json`** with PowerShell so values never reach the console: build a `[ordered]` hashtable, merge with the existing file if present (`Get-Content secrets.json -Raw | ConvertFrom-Json -AsHashtable`), then `ConvertTo-Json -Depth 20 | Set-Content -Encoding utf8 secrets.json`. Fill `_meta` with tenant, subscription, resource group, UTC timestamp, and gaps.

5. **Verify without revealing.** Confirm `secrets.json` parses, run `git check-ignore secrets.json` (it must print the path), and count populated values per section.

6. **Write `docs/environment.md`** with no secret values:
   - Header: subscription name, resource group, region(s), generated timestamp.
   - A table per section: resource name, type, endpoint or URL, and the `secrets.json` key paths available (for example `Foundry:ApiKey`).
   - Model deployments table: deployment name, model, version, SKU. Foundry projects table: project name and project endpoint, because the application calls Foundry through the projects API. These are what planning needs most.
   - App settings: list setting **names** only.
   - Gaps: what could not be read and why.

7. **Report** to the user in a few lines: resources found by type, sections written, masked sample of each key path, gaps, and a reminder that `secrets.json` is gitignored. Mention any value the app likely needs that is still missing (for example no Foundry project or no model deployments, since all AI calls go through a Foundry project).
