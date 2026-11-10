# Text to SQL

A .NET 10 web application that answers plain-English questions about a sample Azure SQL database. A Blazor page (and a JSON API) takes a question, sends it together with the database schema to a Microsoft Foundry model, runs the SQL the model returns, and shows the generated SQL with the first 50 result rows, the rows-affected count, or the error. It is a single-user demo for developers running locally; it has no authentication and runs every generated statement as-is, so point it only at the disposable sample database.

> **Status:** phase 1 of 5 is built (project scaffold, configuration loading, `GET /api/health`, placeholder home page). Everything else below that carries a Planned marker comes from [docs/plan.md](docs/plan.md) and is not implemented yet.

## Prerequisites

- **.NET SDK 10** (the plan records SDK 10.0.401). Check with `dotnet --list-sdks`.
- **Azure resources** (listed in [docs/environment.md](docs/environment.md)):
  - A Microsoft Foundry project and a chat model deployment (default `gpt-5-mini`).
  - An Azure SQL database with Entra-only authentication (the sample database is created in it by the `seed` command).
- **`az login`** to the tenant that holds those resources. The app authenticates with Microsoft Entra ID (`DefaultAzureCredential`), not API keys, so your account needs:
  - the **Foundry User** role (formerly Azure AI User) on the Foundry project, and
  - a user in the Azure SQL database with read, write, and DDL rights (the seed script drops and recreates tables).
- **`secrets.json`** at the repo root (see Setup).

## Setup

1. Produce `secrets.json` with the Claude Code skill `/azure-secrets <resource-group>`. It writes the gitignored `secrets.json` at the repo root and the secret-free inventory in [docs/environment.md](docs/environment.md). Its shape is shown in [secrets.example.json](secrets.example.json).
2. Make sure these keys are present (full list in [docs/configuration.md](docs/configuration.md)):
   - `Foundry:ProjectEndpoint`: the Foundry project endpoint.
   - `Foundry:DefaultDeployment`: the model deployment name.
   - `Database:ConnectionString`: an Azure SQL connection string with `Authentication=Active Directory Default` and no password. *Planned: read from phase 2. `/azure-secrets` records only the server name and databases (`Sql:<server>:Fqdn`, `Sql:<server>:Databases`), not this key, so add it yourself.*
3. Build:

```bash
dotnet build TextToSql.slnx -warnaserror
```

The app finds `secrets.json` by itself: from the `SECRETS_PATH` environment variable if set, otherwise by walking up from the app's folder to the repo root (and, in a git worktree, falling back to the main checkout). See [docs/configuration.md](docs/configuration.md#how-secretsjson-is-found).

## Usage

Run the web app and API. `dotnet run` uses the first launch profile, `http`, which listens on `http://localhost:5212`; add `--launch-profile https` to also listen on `https://localhost:7075`.

```bash
dotnet run --project src/TextToSql.Web
```

The startup log says where `secrets.json` was loaded from, or why it was not (it never logs the contents). On the `http` profile it also logs a harmless warning that the HTTPS redirect port could not be determined.

**Health check** (built):

```bash
curl http://localhost:5212/api/health
```

```json
{"status":"ok"}
```

**Seed or reset the sample database.**

> Planned: not yet implemented (phase 2).

```bash
dotnet run --project src/TextToSql.Web -- seed
```

It drops and recreates Categories, Products, Customers, Orders, and OrderItems with a few hundred deterministic rows, prints the tables and row counts, and exits. Run it again any time generated SQL has changed or dropped data.

**Ask a question through the API.**

> Planned: not yet implemented (phase 3).

```bash
curl -X POST http://localhost:5212/api/query -H "Content-Type: application/json" -d "{\"question\":\"How many customers are there?\"}"
```

The response is JSON with the fields `sql`, `columns`, `rows`, `rowsAffected`, and `error`. Status codes: 200 on success, 400 for an empty or over-long question (the model is not called), 422 when the generated SQL fails (the body includes the SQL and the database error; no retry), and 502 when the model call fails. The exact JSON layout will be documented here once phase 3 is built.

**Ask a question on the website.**

> Planned: not yet implemented (phase 4). Today `/` shows a placeholder page.

Open `http://localhost:5212/`, type a question such as "Top 5 products by total quantity ordered", and press Ask. The page shows the generated SQL and either a results table (up to 50 rows, with a note when capped), the rows-affected count, or the error message.

## Testing

| Tests | Command | Needs |
|---|---|---|
| Unit (default) | `dotnet test TextToSql.slnx --filter "Category!=Live"` | Nothing external. Uses fakes; runs the web host in the `Testing` environment, which never reads `secrets.json`. |
| Live | `dotnet test TextToSql.slnx --filter "Category=Live"` | `az login`, `secrets.json`, Foundry and Azure SQL access. Tagged `[Trait("Category", "Live")]` and skipped when their config is missing. *Planned: none exist yet (phases 2–3).* |

```bash
dotnet test TextToSql.slnx --filter "Category!=Live"
```

Today the unit suite has 11 tests covering the `secrets.json` locator, the configuration precedence order, and `GET /api/health`.

## Project structure

```text
TextToSql.slnx
Directory.Build.props             net10.0 settings: nullable, implicit usings, warnings as errors
src/
  TextToSql.Core/                 Domain types, interfaces, QueryService, prompt, SQL extraction (planned; empty today)
  TextToSql.Web/                  Host: config loading, /api endpoints, Blazor UI, seed command, Azure implementations
    Configuration/                secrets.json locator and configuration-source wiring
    Components/                   Blazor layout and pages
    Program.cs                    Entry point, DI and endpoint mapping
tests/
  TextToSql.Tests/                xUnit tests
    Configuration/                secrets.json locator and precedence tests
    Web/                          WebApplicationFactory endpoint tests
```

## Known limitations

From [docs/plan.md](docs/plan.md#known-limitations):

- **All SQL statement types run unfiltered** (a deliberate decision). Generated `DELETE`, `UPDATE`, or `DROP` statements change or destroy the sample data; run `seed` to restore it. Not suitable for any real database.
- One fixed database from configuration; there is no database selector.
- Only the first result set is returned. Multi-batch scripts (`GO`) are not supported.
- The schema cache is not refreshed after DDL until the app restarts.
- No authentication, rate limiting, or cost or token budget.
- No retry or self-correction when the generated SQL fails.

## Documentation

- [docs/architecture.md](docs/architecture.md): components, main flow, decisions, error handling and security.
- [docs/configuration.md](docs/configuration.md): every configuration key and the precedence order.
- [docs/requirements.md](docs/requirements.md): scope, boundaries, acceptance criteria.
- [docs/plan.md](docs/plan.md): stack, decisions, phases and their status.
- [docs/environment.md](docs/environment.md): the Azure resources (no secrets).
- [docs/development-workflow.md](docs/development-workflow.md): the Claude Code kit and skills used to build this repo.
