# Requirements: Text-to-SQL API and website

Status: Confirmed

## Source prompt

> Build a text to sql system - that accepts question in English and generates and executes sql against selected database and returns the answer
>
> We are building an API with a website - use .net and blazor
> create a sample database to start with

## Problem

A user types a question in plain English on a Blazor website. A .NET Web API sends the question and the database schema to a Microsoft Foundry model, which returns a SQL statement. The API executes that statement against a sample Azure SQL database and returns the generated SQL and the first 50 result rows, which the website displays.

## Users and usage

- A single developer or demo user, running the app locally during development.
- Interactive use through the website; the API can also be called directly.
- No authentication or authorization this session.

## In scope

- A .NET Web API with endpoints to ask a question and to get the generated SQL and results back.
- A Blazor website with a question box that shows the generated SQL, the result rows, or the error.
- A sample database on Azure SQL: a schema and seed script (small retail/sales domain: Customers, Products, Categories, Orders, OrderItems, a few hundred rows) that creates and fills the tables, and can be re-run to reset the data.
- Reading the database schema (table names, columns, types, primary and foreign keys) to give the model context.
- One model call per question through the Foundry projects API to generate SQL.

## Out of scope

- A Foundry agent (plain model calls only).
- A natural-language summary of the results.
- Automatic retry or self-correction when SQL fails.
- Selecting among multiple databases, or adding databases from the UI.
- Database engines other than Azure SQL.
- Authentication and authorization, query history and saved queries, charts, and conversational follow-up questions.
- Hosting and deployment (handled separately by `/deploy-pipeline`).

## Inputs and outputs

- **Input:** a question in English (non-empty; assumed max 1,000 characters).
- **Output (success):** the generated SQL and up to the first 50 result rows (column names and values). For statements that return no rows (for example `DELETE`), the output is the generated SQL and the number of rows affected.
- **Output (failure):** the generated SQL (if any) and the error message, shown to the user. Failures include an empty question, a model error, and a SQL execution error.
- **API:** JSON over HTTP. Assumed shape: `POST /api/query` with `{ "question": "..." }`, returning `{ sql, columns, rows, rowsAffected, error }`. The exact contract will be set in the plan.

## AI behavior

- Plain model calls (chat completions) through the Foundry project endpoint, authenticated with Microsoft Entra ID (`DefaultAzureCredential`); not the Assistants API and not a Foundry agent.
- Deployment: `Foundry:DefaultDeployment` from `secrets.json`. Assumption: confirmed once `docs/environment.md` exists.
- The model receives only the schema (table names, columns, types, keys) and the question. It decides the SQL text only.
- Code decides everything else: it reads the schema, builds the prompt, extracts the SQL from the response, executes it, caps the rows at 50, and formats the output.
- The prompt lives in a dedicated file or constant, and the model client sits behind an interface so tests use a fake.

## Boundaries

- **All SQL statement types are allowed for now**, including `SELECT`, `INSERT`, `UPDATE`, `DELETE`, and `DROP`. This is a deliberate decision by the user. Risk: a generated statement can change or destroy the sample data or schema. Mitigation: the database holds sample data only, and the seed script can recreate it. This must be recorded as a known limitation in `docs/plan.md`.
- No row data, connection strings, or secrets are ever sent to the model; only the schema and the question.
- Never print secrets in logs, UI, or test output.
- Assumption: every query runs with a command timeout (proposed 30 seconds) so a runaway query cannot hang the API.
- Assumption: Azure SQL is reached with Entra ID authentication (the developer's `az login` locally) rather than a SQL password, matching the Foundry auth approach. To be confirmed against the Azure SQL resource.

## Quality bar

- Errors appear in the UI as readable messages, never stack traces.
- Structured logging (`ILogger`) records the question, the generated SQL, timings, and errors; it never records result data or secrets.
- No formal performance target. Assumption: a typical question returns in under 10 seconds.
- Unit tests use fakes for the model and the database. Live tests that hit Foundry or Azure SQL are tagged `Live` and skipped when their config is missing.

## Assumptions

- An Azure SQL server and database exist, or will be listed, in the resource group that `/azure-secrets` inventories. `docs/environment.md` does not exist yet, so this is not verified.
- "Built-in test database" means one fixed sample database configured in settings. There is no database selector in the UI or API this session.
- The sample database is created by a seed script that the app or a command runs. The plan will decide which.
- The input limit of 1,000 characters, the 30-second timeout, and the API shape above are proposals, not user decisions.

## Acceptance criteria

- **AC1:** Given the sample database is seeded, when the seed script is run again, then the tables are recreated with the same sample data.
- **AC2:** Given a configured database, when the schema is read, then the result contains every user table with its columns, data types, primary keys, and foreign keys.
- **AC3:** Given a question, when the API builds the model request, then the request contains the question and the schema, and contains no row data, connection string, or secret.
- **AC4:** Given a model response, when the SQL is extracted, then the SQL text is returned without markdown fences or surrounding commentary.
- **AC5:** Given a question whose generated SQL returns more than 50 rows, when it is executed, then the response contains the generated SQL, the column names, and exactly the first 50 rows.
- **AC6:** Given a question whose generated SQL is a non-query statement (for example `DELETE`), when it is executed, then the statement runs and the response contains the generated SQL and the number of rows affected.
- **AC7:** Given generated SQL that fails to execute, when the API returns, then the response contains the generated SQL and the database error message, the HTTP status indicates failure, and no retry is attempted.
- **AC8:** Given an empty or whitespace-only question, when it is submitted, then the API returns a validation error without calling the model.
- **AC9:** Given the model call fails (for example auth or network failure), when the API returns, then the response contains a readable error message and no stack trace.
- **AC10:** Given the website, when the user submits a question, then the page shows the generated SQL and either the results table (up to 50 rows), the rows-affected count, or the error message.
- **AC11 (Live):** Given valid Foundry and Azure SQL configuration, when the question "How many customers are there?" is asked, then the generated SQL executes and returns the customer count from the sample database.

## Open questions

- Does an Azure SQL database exist in the resource group, and what authentication does it allow (Entra ID or SQL login)? This depends on running `/azure-secrets` first.
- Which Foundry model deployment should be used? This depends on `docs/environment.md`.
- Is the single fixed database (no selector) the intended reading of "built-in test database"?
