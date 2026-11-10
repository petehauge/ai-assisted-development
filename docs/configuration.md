# Configuration

The app uses standard ASP.NET Core configuration with one extra source, `secrets.json`, for local development. Keys are written here as configuration paths (`Section:Key`). As environment variables, replace `:` with `__` (for example `Foundry__ProjectEndpoint`); on the command line use `--Section:Key=value`.

This page lists key paths only. Real values live in the gitignored `secrets.json` (shape in [secrets.example.json](../secrets.example.json)) or in the hosting environment; the Azure resource names are in [environment.md](environment.md).

## Precedence

Later sources win:

1. `src/TextToSql.Web/appsettings.json`, then `appsettings.{Environment}.json` (committed defaults).
2. `secrets.json` (local development only; skipped in the `Testing` environment).
3. Environment variables.
4. Command-line arguments.

This order is implemented in `SecretsConfigurationExtensions.AddSecretsFile`, which inserts `secrets.json` just before the environment-variables source, and is covered by `ConfigurationOrderTests`.

## How `secrets.json` is found

Implemented in `SecretsFileLocator`:

1. If the `SECRETS_PATH` environment variable is set, use that file. If the file does not exist, nothing is loaded and the startup log says so; the app does not fall back to searching.
2. Otherwise walk up from the app's output folder (`AppContext.BaseDirectory`) to the repo root, the first folder that contains a `.git` folder or a `.git` file, and use `secrets.json` there.
3. If the repo root is a git worktree (`.git` is a file) with no `secrets.json`, follow its `gitdir:` line (`<main checkout>/.git/worktrees/<name>`) and use `secrets.json` in the main checkout.
4. If nothing is found, the app still starts and logs a warning with the reason.

The startup log line names the file used (its path, never its contents).

## Keys

| Key path | Purpose | Required | Default | Where it is set | Read by | State |
|---|---|---|---|---|---|---|
| `Logging:LogLevel:Default` | Default log level | No | `Information` | `appsettings.json` | ASP.NET Core logging | Built |
| `Logging:LogLevel:Microsoft.AspNetCore` | Log level for framework messages | No | `Warning` | `appsettings.json` | ASP.NET Core logging | Built |
| `AllowedHosts` | Host filtering | No | `*` | `appsettings.json` | ASP.NET Core host filtering | Built |
| `Foundry:ProjectEndpoint` | Foundry project endpoint for model calls | Yes (from phase 3) | none | `secrets.json` (written by `/azure-secrets`), env var, CLI | `FoundrySqlGenerator` | Planned (phase 3) |
| `Foundry:DefaultDeployment` | Model deployment name used for SQL generation | Yes (from phase 3) | none | `secrets.json` (written by `/azure-secrets`), env var, CLI | `FoundrySqlGenerator` | Planned (phase 3) |
| `Database:ConnectionString` | Azure SQL connection string with `Authentication=Active Directory Default`; no password belongs in it | Yes (from phase 2) | none | `secrets.json` (add it by hand; see below), env var, CLI | `SqlSchemaProvider`, `SqlServerExecutor`, `seed` | Planned (phase 2) |
| `Query:MaxRows` | Maximum rows returned from the first result set | No | `50` | `appsettings.json`, env var, CLI | `SqlServerExecutor` / `QueryService` | Planned (phase 3) |
| `Query:CommandTimeoutSeconds` | SQL command timeout | No | `30` | `appsettings.json`, env var, CLI | `SqlServerExecutor` | Planned (phase 3) |
| `Query:MaxQuestionLength` | Maximum question length before a 400 response | No | `1000` | `appsettings.json`, env var, CLI | `/api/query` and the Blazor page | Planned (phase 3) |

> Planned: not yet implemented (phases 2–3). Only the `Logging` and `AllowedHosts` keys are read by the code today; the rest are from [plan.md](plan.md#architecture) and will be checked against the code when those phases are built. Phase 5 (optional) makes a missing required key produce a clear error that names it.

### Environment variables that are not configuration keys

| Variable | Purpose | Read by |
|---|---|---|
| `SECRETS_PATH` | Explicit path to `secrets.json`; overrides the repo-root search | `Program.cs` → `SecretsFileLocator` |
| `ASPNETCORE_ENVIRONMENT` | `Development` (set by both launch profiles) or `Testing` (set by the test host, which skips `secrets.json`) | ASP.NET Core host |

### Launch profiles

`src/TextToSql.Web/Properties/launchSettings.json`:

| Profile | URLs |
|---|---|
| `http` (used by `dotnet run` because it is listed first) | `http://localhost:5212` |
| `https` (`--launch-profile https`) | `https://localhost:7075`, `http://localhost:5212` |

## Notes on `Database:ConnectionString`

- `/azure-secrets` does not produce this key: for the SQL server it records only `Sql:<server>:Fqdn` and `Sql:<server>:Databases`. Build the connection string from those (server `tcp:<fqdn>,1433`, the database name, `Authentication=Active Directory Default`, `Encrypt=True`) and add it under `Database` in `secrets.json`.
- The hosted web app in [environment.md](environment.md) already has an app setting named `Sql__ConnectionString`, which maps to the key `Sql:ConnectionString`, not `Database:ConnectionString`. A hosted deployment needs either a `Database__ConnectionString` setting or a change to the key the code reads. This is an open point for phase 2 and the deployment pipeline (`docs/deployment.md`, once `/deploy-pipeline` writes it), not decided here.

## Other keys in `secrets.json`

`secrets.json` also holds inventory and secret values for other resources (for example `Foundry:ApiKey`, `Foundry:Projects:<name>:Endpoint`, `Storage:*`, `AISearch:*`). The app does not read them. In particular it does not use `Foundry:ApiKey`: it authenticates to Foundry with Microsoft Entra ID.
