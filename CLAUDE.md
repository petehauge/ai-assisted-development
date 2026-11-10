# CLAUDE.md

This repository starts empty and is built live during a timeboxed (~60 minute) AI-assisted development session. The goal is a working, tested application delivered in small phases, with clear reasoning visible in `docs/`. The technology stack is decided during planning and recorded in `docs/plan.md`; do not assume one before then.

## Accuracy and integrity — CRITICAL

Being accurate matters more than being fast. This overrides any pull toward a quick, confident answer.

- **Never present something as true unless you verified it.** No guessing dressed up as fact, no misrepresenting what you did or what happened. Saying "I don't know" or "I haven't checked that" is always acceptable.
- **Prefer current documentation over memory** for parameters, configuration, CLI flags, package and action versions, SDK APIs, and setup steps. Read the official docs or the tool's own `--help` before relying on a detail you are not sure of.
- **If you don't know, ask.** When information is missing or a decision is ambiguous, ask the user instead of inventing a plausible answer.
- **Label what you say:** distinguish what you verified (read, ran, observed), what you inferred, and what you assumed.
- **Claims of success need evidence.** "It builds", "tests pass", or "it works" must reflect a command you actually ran and whose output you checked. If you skipped a step or something failed, say so plainly with the output.

## Workflow

Follow this sequence. Each step has a project skill; use it.

1. **Environment** (`/azure-secrets <resource-group>`): pull Azure resource details and secrets into `secrets.json` and write the non-secret inventory to `docs/environment.md`.
2. **Clarify** (`/clarify`): turn the prompt into questions, then write `docs/requirements.md` with scope and boundaries. Do not plan until the user confirms the requirements.
3. **Plan** (`/plan-phases`): write `docs/plan.md` with the stack, architecture, decisions, the build/test/run commands, and phases. Do not write application code until the user approves the plan.
4. **Build** (`/implement-phase N`, or a range such as `/implement-phase 2-3`): phases run in order, and each phase is built and verified before the next one starts. A single phase is reviewed and committed on its own. A range is reviewed once after its last phase and committed as one `phase-<first>-<last>:` commit. **Phase 1 always creates the project(s)**: solution or package structure, test project, build settings, configuration loading, and an entry point that runs. That gives documentation and pipeline work a real structure to target from then on.
5. **In parallel, once their inputs exist:** documentation (`/write-docs`) after the plan is approved; the deployment pipeline (`/deploy-pipeline`) after phase 1; review (`/production-review`) after the first end-to-end phase.

Prefer the simplest design that meets the requirements. Record shortcuts as known limitations in `docs/plan.md` rather than gold-plating.

## Definition of done (every phase)

- The build command recorded in `docs/plan.md` succeeds with no warnings (configure warnings as errors where the toolchain supports it).
- The test command passes, and any tests the phase added are meaningful (see Testing).
- The phase's demo command runs end to end and its output was checked, not assumed.
- The phase's status in `docs/plan.md` is updated to Done with a one-line note.
- The user has reviewed the changes and said to commit, and the work is committed with a message starting `phase-N:`, or `phase-<first>-<last>:` for a range reviewed together (see Review before commit).

## Review before commit — CRITICAL

Never commit until the user has reviewed the changes and told you to commit. This applies to every skill and every session, including steps a skill describes as "commit".

1. **Make the changes** and verify them (build, tests, demo, as the skill requires).
2. **Prepare for review**, then stop: list the changed files (`git status`, `git diff --stat`), summarize what changed and why, give the verification results, and propose the commit message and the exact paths that would be committed. Do not stage or commit.
3. **Iterate.** The user reviews, often by leaving comments in the editor that `/local-comments` handles. Fix what they ask for and prepare for review again. Fixing comments is not permission to commit.
4. **Commit only when the user says so** in chat (for example "commit" or "looks good, commit"), and only the paths you proposed.

The one exception: the user explicitly says to commit without review (for example "implement phases 2-3 and commit without review"). That permission covers only the work named in that request, not later requests. Approving a requirements summary or a plan is approval of its content, not permission to commit. Never push without being asked.

## Repository layout

| Path | Contents |
|---|---|
| `src/` | Application code (one folder per project or package) |
| `tests/` | Automated tests, mirroring `src/` |
| `docs/` | Requirements, plan, environment inventory, architecture and configuration docs |
| `.github/workflows/` | CI/CD pipelines |
| `reports/` | Generated review output (gitignored) |
| `.claude/skills/` | Project skills for this workflow |

Stack-specific conventions (language version, project files, shared build settings, test framework) are decisions recorded in `docs/plan.md`, not rules here. Whatever the stack: put external services (LLM clients, Azure SDK clients, file system, clock, process execution) behind small interfaces so tests can substitute fakes, pass cancellation through long-running or I/O work where the language supports it, and never swallow exceptions silently.

## Testing

- Add tests where they earn their place: to protect important behavior, verify contracts between components, cover the main path and meaningful error cases, and guard against drift from the behavior defined in `docs/requirements.md`. Do not add tests just to raise a count or to restate the implementation; trivial getters, framework plumbing, and duplicates of existing coverage don't need tests.
- When a phase adds behavior that meets that bar, its tests go in the same commit.
- Unit tests never call real Azure or LLM endpoints and never read `secrets.json`. Use fakes or mocks behind the interfaces above.
- Live integration tests are allowed but must be tagged `Live` using the test framework's mechanism (for example xUnit `[Trait("Category", "Live")]` or a pytest marker) and skipped when the needed config is missing. The default test command excludes them; record both commands in `docs/plan.md`.
- For .NET projects, the `dotnet-test` plugin skills (test gap analysis, untested sources, anti-patterns) are available; use them when finishing a phase.

## Secrets and configuration

- `secrets.json` at the repo root holds all resource details and secrets for local development; a hosted app gets its settings from the Azure resource's configuration and its identity instead. It is gitignored and is produced by `/azure-secrets`. Its shape is documented in `secrets.example.json` (committed, no real values).
- You may read `secrets.json` to wire up configuration. **Never print secret values** in chat, logs, test output, docs, or commit messages. Refer to them by key path, for example `Foundry:ApiKey`. When you must show a value, mask it (first 4 characters, then `…`).
- Application configuration order (later wins): committed defaults file (for example `appsettings.json` or `config.json`) → `secrets.json` (from the path in the `SECRETS_PATH` environment variable if it is set; otherwise found by walking up from the application's location and stopping at the repo root, the first folder that contains a `.git` folder or a `.git` file; in a git worktree `.git` is a file whose `gitdir:` line points to `<main checkout>/.git/worktrees/<name>`, so if `secrets.json` is not in the worktree root, look in that main checkout) → environment variables → command-line arguments. Missing optional config must produce a clear error message, not a crash.
- Do not create files outside this repository. If the user later wants specific values moved to a platform secret store, do it only when asked.
- Never commit `secrets.json`. Before any commit, check `git status` to confirm it is not staged.

## Azure

- `docs/environment.md` lists the available resources (names, endpoints, model deployments) without secrets. Read it before planning anything that uses Azure.
- Treat Azure as read-only: list and show, never create, update, or delete resources unless the user explicitly asks.
- Before reading keys, confirm the active tenant and subscription with the user (`az account show`).

## AI agents, models, and LLMs: Microsoft Foundry

- **All AI in the application goes through Microsoft Foundry.** Every agent, model, and LLM call targets the Foundry resource, project, and model deployments listed in `docs/environment.md`. Use those deployment names and endpoints, and read credentials from `secrets.json` by key path (`Foundry:*`). Don't call a model provider directly or invent a deployment name; if the one you need isn't listed, ask.
- **Use the Foundry projects API in every case, never the old Assistants API.** Agents, threads/conversations, and model calls go through the Foundry project endpoint (`Foundry:ProjectEndpoint` in `secrets.json`, with the default model deployment in `Foundry:DefaultDeployment`; every project is also listed under `Foundry:Projects:<name>`). Don't use the legacy Assistants API, or SDKs and samples built on it, even if they look simpler.
- **Authenticate with Microsoft Entra ID, not API keys.** The Foundry projects SDK and API authenticate with a token credential (for example `DefaultAzureCredential`): locally that is the developer's `az login`, and when hosted it is the app's managed identity. The identity needs the **Foundry User** role (formerly Azure AI User) on the Foundry project. API keys (`Foundry:ApiKey`) work only on the OpenAI-compatible `/openai/v1` endpoint, so use them only if the user explicitly chooses that path. Source: https://learn.microsoft.com/en-us/azure/foundry/how-to/develop/sdk-overview (checked 2026-11-09); re-check it before relying on this, per the next rule.
- **Don't rely on training data for Foundry or agent calls.** Foundry's REST API, SDKs, API versions, and authentication change often. Before writing or changing any Foundry or agent call, read the current official documentation (Microsoft Learn, including the REST API reference) for the exact endpoint path, `api-version`, request and response shape, and the authentication it requires. Record the doc URL you used in the relevant decision in `docs/plan.md`. If the docs and your memory disagree, the docs win; if the docs are unclear, say so and ask.
- Keep prompts and agent instructions in dedicated files or constants, not scattered inline, and keep every Foundry call behind an interface so tests use fakes.

## Parallel sessions and file ownership

Several sessions may run at once. Stay inside your lane:

| Work | May write |
|---|---|
| Environment (`/azure-secrets`) | `secrets.json` (never committed), `docs/environment.md` |
| Clarify (`/clarify`) | `docs/requirements.md` |
| Plan (`/plan-phases`) | `docs/plan.md` (after approval, implementation owns the phase status lines) |
| Implementation (`/implement-phase`) | `src/`, `tests/`, solution/package and build files, phase status in `docs/plan.md` |
| Pipeline (`/deploy-pipeline`) | `.github/workflows/`, `docs/deployment.md`, GitHub repository secrets (see the skill) |
| Documentation (`/write-docs`) | `README.md`, `docs/` except `requirements.md`, `plan.md`, `environment.md`, and `deployment.md` |
| Review (`/production-review`, `/code-review`, `/security-review`) | `reports/` and `CLAUDE-SECURITY-*/` only (both gitignored); never edits code |

Only the implementation session commits code. Other sessions commit only their own files. Every session commits only after the user's review (see Review before commit). Sessions that share one working folder also share git's staging area, so a plain `git commit` can sweep in files another session staged. Always commit with explicit paths: `git add -- <your paths>` (needed for new files), then `git commit -m "<message>" -- <your paths>`, which commits only those paths and leaves anything else staged where it is.

**Worktrees and `secrets.json`.** `secrets.json` lives only in the main checkout, never copied into a worktree. Work that needs Azure or `secrets.json` runs in the main checkout: `/azure-secrets`, Live tests, demo commands that call Foundry, and `/deploy-pipeline`. Use a git worktree only for phases the plan marks as independent and that need fakes only. The configuration loader still finds the main checkout's `secrets.json` from a worktree (see Secrets and configuration), so an occasional Live run from a worktree works without copying the file.

## Review tools available

- Project skill `/production-review`: full production readiness review with an HTML report.
- Plugins: `pr-review-toolkit` (code-reviewer, silent-failure-hunter, pr-test-analyzer, type-design-analyzer, comment-analyzer, code-simplifier), `claude-security` (verified security scan).
- Built in: `/code-review`, `/security-review`, `/simplify`.
