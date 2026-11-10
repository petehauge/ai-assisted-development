---
name: write-docs
description: Write or refresh project documentation - README.md, docs/architecture.md with Mermaid diagrams, and docs/configuration.md - from the plan, requirements, environment inventory, and current code. Safe to run in a parallel session while implementation continues. Use when the user asks for docs, a README, architecture or usage documentation, or to sync docs with the code.
argument-hint: "[sync]"
---

# Write documentation

Mode: `$ARGUMENTS` (empty = write or refresh everything; `sync` = only bring existing docs in line with the current code).

## Ownership

You may write only `README.md`, `docs/architecture.md`, `docs/configuration.md`, and other new files under `docs/`. Never edit `docs/requirements.md`, `docs/plan.md`, `docs/deployment.md` (owned by `/deploy-pipeline`), `src/`, `tests/`, or `.github/`; link to `docs/deployment.md` from the README if it exists. Never include secret values; refer to config by key path.

## Sources, in order of authority

1. The code in `src/` and `tests/` (what actually exists wins).
2. `docs/plan.md` (intended design, phases, decisions, limitations).
3. `docs/requirements.md` (purpose, scope, boundaries).
4. `docs/environment.md` and `secrets.example.json` (configuration keys; never read values from `secrets.json` into docs).

If the code does not exist yet (running in parallel with phase 1), write from the plan and mark unbuilt parts with `> Planned: not yet implemented (phase N).` Remove those markers on later runs as phases complete.

## Documents

**README.md**
- One-paragraph overview: what it does, who it is for.
- Prerequisites (runtime or SDK version for the chosen stack, Azure resources, `secrets.json`, and `az login` with the Foundry User role on the Foundry project, since the app authenticates with Microsoft Entra ID).
- Setup: how to produce `secrets.json` (`/azure-secrets`) and which keys are required.
- Usage: the real CLI syntax with two or three realistic examples and sample output.
- Testing: unit vs Live tests and the commands to run each.
- Project structure: a short tree of `src/` and `tests/` with one line each.
- Known limitations (from the plan).
- Links to the docs below.

**docs/architecture.md**
- Component overview with a Mermaid diagram that matches the actual projects and interfaces.
- A Mermaid sequence diagram of the main flow (input → processing → AI call → output).
- Key components: responsibility, key types, extension points.
- Decisions: summarize from `docs/plan.md` and link to it rather than duplicating.
- Error handling, logging, and security notes (what data is sent to the model, how secrets are handled).

**docs/configuration.md**
- Every configuration key: path, purpose, required or optional, default, and where it is read (appsettings, `secrets.json`, env var, CLI flag).
- Configuration precedence order.

## Verify before finishing

- Every CLI command, flag, and config key you documented exists in the code (search for it). If not built yet, it must carry the Planned marker.
- Every Mermaid block is syntactically valid (simple `flowchart`/`sequenceDiagram` syntax, no unescaped special characters in labels).
- No secret values appear anywhere: search the files you wrote for `key`, `password`, `connectionstring`, and `AccountKey=` and confirm only key paths or placeholders appear.

## Review, then commit

Prepare the changes for review, following Review before commit in `CLAUDE.md`: report which files you created or updated, what is still marked Planned, and the proposed commit message. Don't commit until the user says so.

When they do, commit only the files you wrote, by explicit path, because the implementation session may have files staged in the same working folder: `git add -- <your files>`, then `git commit -m "docs: <summary>" -- <your files>`. Confirm `secrets.json` is not among them.
