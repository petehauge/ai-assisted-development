# Review area checklists

Each reviewer applies its section. Report only concrete, evidenced problems; a checklist item that is fine produces no finding. Judge severity by production impact for this application, not in the abstract.

## Security

- Untrusted input reaching file paths, shell or process execution, SQL, deserialization, or URLs without validation (path traversal, command injection, SSRF).
- Prompt injection: content from analyzed files or users concatenated into LLM prompts without separation, and model output trusted to drive actions (file writes, commands) without validation.
- Secrets in source, logs, exceptions, test fixtures, or prompts sent to a model.
- Authentication and authorization on any exposed endpoint; TLS enforced for outbound calls.
- Dependency risk: outdated or vulnerable packages (the stack's audit tool, for example `dotnet list package --vulnerable`, `npm audit`, or `pip-audit`), unpinned versions.

## Reliability and error handling

- Swallowed exceptions, empty catches, catch-all blocks that hide failures, fallbacks that silently produce wrong results.
- Outbound calls (LLM, Azure, HTTP) without timeouts, retries with backoff for transient errors, or handling of rate limiting (429) and partial failures.
- Missing `CancellationToken` propagation; blocking on async (`.Result`, `.Wait()`).
- Resource leaks: undisposed `HttpClient`/streams, `HttpClient` created per call instead of via `IHttpClientFactory` or a shared instance.
- Exit codes and error messages for the CLI: does a failure produce a non-zero exit and an actionable message?
- Non-deterministic LLM output: is the response parsed defensively and validated against a schema?

## Performance

- Unbounded reads of large files into memory; missing streaming.
- Sequential I/O or LLM calls that could safely run concurrently (with a bounded degree of parallelism).
- Repeated work that could be cached (re-reading files, re-creating clients, re-tokenizing).
- Inefficient collections or LINQ in hot paths; excessive allocations; string concatenation in loops.
- Prompt size: sending more context than needed, which costs latency and tokens.

## Scalability

- Behavior with 10x and 100x the expected input (files, repo size, requests): does memory or time grow unbounded?
- Concurrency limits and back-pressure; respecting model and Azure rate limits and quotas.
- Global mutable state or singletons that block running multiple instances.
- Chunking and batching strategy for large inputs; pagination for Azure listings.

## Maintainability and code quality

- Clear separation of concerns; external dependencies behind interfaces; no god classes.
- Naming, dead code, duplication, magic strings and numbers, prompts scattered inline.
- Nullable reference warnings suppressed rather than fixed; `!` used to silence the compiler.
- Consistency with `CLAUDE.md` conventions and the architecture in `docs/plan.md`.
- Type design: invariants enforced by types, immutable records for data, minimal public surface.

## Test quality and coverage

- Acceptance criteria in `docs/requirements.md` without a corresponding test.
- Tests that assert nothing meaningful, test implementation details, or depend on order, time, or network.
- Missing negative and edge-case tests (empty input, huge input, malformed model output, API failure).
- Unit tests that hit real services; Live tests not tagged `Category=Live`.

## Observability

- Structured logging via `ILogger` with useful context (operation, input identifiers, durations), not `Console.WriteLine` scattered through logic.
- Log levels appropriate; no secrets or full prompts with sensitive content at Information level.
- Ability to diagnose a failed run: correlation of LLM calls, token usage and latency recorded, error details preserved.
- Metrics or tracing hooks (OpenTelemetry or Application Insights) if the app is long-running or hosted.

## Compliance and privacy

- What data leaves the machine: source code, file contents, or PII sent to an external model. Is it documented and controllable (opt-in, redaction, allowlists)?
- Data residency: which region the model and storage run in, relative to requirements.
- Retention: are prompts, responses, or reports stored, where, and for how long?
- Licensing of dependencies (copyleft packages in a distributed tool).
- Audit trail for AI-generated findings or decisions where the requirements call for it.

## Configuration and secrets

- Secrets only from configuration (`secrets.json`, environment, Key Vault), never hard-coded.
- `secrets.json` and other local secret files gitignored; no secrets in committed `appsettings*.json`.
- Clear validation of required configuration at startup with actionable errors (options validation, `ValidateOnStart`).
- Sensible defaults; environment-specific overrides possible without code changes.
- Managed identity / `DefaultAzureCredential` considered where keys are used.

## Operability

- How it is built, packaged, and run outside the dev machine (for example `dotnet publish`, `npm run build`, a container image, a packaged CLI tool, or the CI/CD workflows in `.github/workflows/`).
- Health checks or a self-test command if hosted; graceful shutdown.
- CI readiness: deterministic build, tests runnable without secrets, Live tests separable.
- Versioning and a `--version` / `--help` that are accurate.

## Cost

- Tokens per run: prompt size, number of calls, model choice relative to task difficulty.
- Guardrails: maximum files, maximum tokens, budget or dry-run mode, caching of repeated analyses.
- Azure resource SKUs and anything billed per call or per hour that the app depends on.
- Cost visibility: is token usage reported to the user at the end of a run?
