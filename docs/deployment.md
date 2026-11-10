# Deployment

The GitHub Actions workflow [`.github/workflows/deploy-webapp.yml`](../.github/workflows/deploy-webapp.yml) builds and tests the solution, publishes `src/TextToSql.Web`, and deploys it to Azure App Service.

## Target

| | |
|---|---|
| Resource | Web app `aiasst-web-m4gvse` (App Service, Linux, `DOTNETCORE\|10.0`) |
| Resource group | `ai-assisted-staging-rg` (subscription `4c36dcce-4d41-41d6-af6d-d12040771ba8`) |
| Region | westus3 |
| URL | https://aiasst-web-m4gvse.azurewebsites.net |

## Triggers

| Event | build-and-test | deploy |
|---|---|---|
| Push to `main` | Yes | Yes |
| Pull request to `main` | Yes | No |
| Manual run (`workflow_dispatch`) | Yes | Yes, if run on `main` |

## Jobs

1. **build-and-test** (`ubuntu-latest`, .NET SDK `10.0.x`): `dotnet restore`, then `dotnet build TextToSql.slnx -c Release -warnaserror`, then `dotnet test --filter "Category!=Live"`. Live tests are excluded because the pipeline has no `secrets.json` and no Azure identity. Finally `dotnet publish src/TextToSql.Web`, with the output uploaded as the `webapp` workflow artifact.
2. **deploy** (needs build-and-test): downloads the `webapp` artifact and deploys the folder with `azure/webapps-deploy@v3` (zip deploy).

Workflow permissions are `contents: read` only. No `id-token` permission is needed because the workflow does not use OIDC.

Action versions were checked on 2026-11-10 against each action's GitHub releases and its `action.yml`/README: `actions/checkout@v7`, `actions/setup-dotnet@v6`, `actions/upload-artifact@v7`, `actions/download-artifact@v8`, `azure/webapps-deploy@v3`.

## Authentication

The workflow authenticates with the web app's **publish profile**, which is scoped to this one app. No Entra app registration or federated credential is used. The publish profile works only while SCM basic-auth publishing credentials are enabled on the app; on 2026-11-10 `basicPublishingCredentialsPolicies/scm` reported `allow: true`.

### Repository secrets

| Secret | Contents | Source |
|---|---|---|
| `AZURE_WEBAPP_PUBLISH_PROFILE` | Publish profile XML for `aiasst-web-m4gvse` | `az webapp deployment list-publishing-profiles --name aiasst-web-m4gvse --resource-group ai-assisted-staging-rg --xml` |

To rotate it (for example after resetting the publish profile in the portal), pipe the value straight into `gh` so it is never printed:

```bash
az webapp deployment list-publishing-profiles --name aiasst-web-m4gvse --resource-group ai-assisted-staging-rg --xml | gh secret set AZURE_WEBAPP_PUBLISH_PROFILE --repo petehauge/ai-assisted-development
```

## Runtime settings on the web app

The hosted app has no `secrets.json`. It reads configuration from App Service app settings (environment variables, with `__` for `:`), and authenticates to Azure with its system-assigned managed identity (principal `ebd0bd21-714f-459c-9b88-0bb19785cff9`) through `DefaultAzureCredential`.

| Setting | Needed for | Status (checked 2026-11-10) |
|---|---|---|
| `Foundry__ProjectEndpoint` | Foundry model calls (phase 3) | Present: `https://aiasst-foundry-m4gvse.services.ai.azure.com/api/projects/aiasst-project` |
| `Foundry__DefaultDeployment` | Foundry model calls (phase 3) | Present: `gpt-5-mini` |
| `Database__ConnectionString` | Azure SQL (phase 2 onward); the plan's key is `Database:ConnectionString` | **Missing.** The app has `Sql__ConnectionString` instead, which the app does not read. Either add `Database__ConnectionString` (same value as `Sql__ConnectionString`), or change the app's key. Not changed by this pipeline work. |
| `Query__MaxRows`, `Query__CommandTimeoutSeconds`, `Query__MaxQuestionLength` | Optional overrides | Not needed; defaults come from `appsettings.json`. |

### Identity and roles

| Requirement | Status |
|---|---|
| Managed identity **Foundry User** role on the Foundry project | Present at the Foundry account scope `aiasst-foundry-m4gvse` (from `az role assignment list --assignee <principal> --all`), which covers the project. |
| Managed identity as a user in database `appdb` on `aiasst-sql-w3-m4gvse` with read/write (and DDL, since generated SQL may run DDL) | **Not verified.** It cannot be checked with `az`. If it is missing, an Entra admin of the SQL server runs, in `appdb`: `CREATE USER [aiasst-web-m4gvse] FROM EXTERNAL PROVIDER; ALTER ROLE db_datareader ADD MEMBER [aiasst-web-m4gvse]; ALTER ROLE db_datawriter ADD MEMBER [aiasst-web-m4gvse]; ALTER ROLE db_ddladmin ADD MEMBER [aiasst-web-m4gvse];` |
| SQL server firewall allows Azure services | Not verified. |

No Azure settings, identities, or role assignments were changed while setting up the pipeline.

## Running the workflow by hand

```bash
gh workflow run deploy-webapp.yml --repo petehauge/ai-assisted-development --ref main
```

```bash
gh run watch --repo petehauge/ai-assisted-development
```

Or go to GitHub → Actions → **Build and deploy web app** → **Run workflow**.

After a deploy, check the app with `curl https://aiasst-web-m4gvse.azurewebsites.net/api/health`, which should return `{"status":"ok"}`.
