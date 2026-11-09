---
name: plan-phases
description: Write docs/plan.md from confirmed requirements - architecture, key decisions, and a sequence of small phases that are each runnable and tested on their own, with demo commands and parallel work streams. Use after requirements are agreed, or when the user asks for a plan, phases, or architecture.
---

# Plan in phases

## Preconditions

- `docs/requirements.md` exists and the user has confirmed it. If not, suggest `/clarify` first.
- Read `docs/requirements.md`, `docs/environment.md` (if present), and `CLAUDE.md`.

## Steps

1. **Choose the stack and record its commands.** If the requirements or the user haven't fixed the language and framework, propose one with a short reason and confirm it. Record the exact **build**, **default test** (Live tests excluded), **Live test**, and **run** commands; every later phase, review, and pipeline uses them. Check current documentation for versions and tooling rather than relying on memory.

2. **Design the smallest architecture that meets the requirements.** List the projects, key components and interfaces, and the data flow. Include one Mermaid diagram (component or flow). Put every external dependency (LLM, Azure, file system, process execution) behind an interface.

3. **Record decisions.** For each significant choice (stack, libraries, test framework, Foundry deployment and agent vs plain model call, output format, how AI results are validated, hosting target), write a short entry in the Decisions section: decision, options considered, why, consequences. Three to six decisions is typical.

4. **Cut phases.** Target 4–6 phases, each roughly 8–12 minutes of work. Rules:
   - **Phase 1 creates the project(s)**: app and test projects in the chosen stack, shared build settings (warnings as errors where supported, for example `Directory.Build.props` in .NET), configuration loading in the order given in `CLAUDE.md`, logging, an entry point that runs, and at least one meaningful passing test. Documentation and the deployment pipeline start from this structure, so keep phase 1 small and get it done early.
   - Every phase ends in a **runnable, demonstrable** state with a concrete demo command and the expected output.
   - Every phase lists the **tests** it adds, mapped to acceptance criteria from `docs/requirements.md`. Plan only tests that meet the bar in the Testing section of `CLAUDE.md`.
   - Order phases so the riskiest integration (usually the first real Foundry call) happens early, behind an interface with a fake for tests. Per `CLAUDE.md`, it uses the Foundry projects API, and its decision cites the current documentation it was built from.
   - Real AI or Azure calls get a Live-tagged integration test; everything else uses fakes.
   - Keep a final phase for hardening (error handling, limits, output polish) only if time allows; it is the first thing to cut.

5. **Mark parallel work.** In the Parallel streams section, say what can run alongside implementation and from when: documentation (`/write-docs`) once the plan is approved; the deployment pipeline (`/deploy-pipeline`) once phase 1 is Done; review (`/production-review`) after the phase that produces the first end-to-end path. Mark any phases that are independent of each other and could run in separate worktrees (most are sequential; say so if none are independent). Phases that are small and closely related can be suggested as a range for `/implement-phase` (for example `2-3`).

6. **List risks and known limitations**: what might not fit in the time, and what is deliberately left out.

7. **Write `docs/plan.md`** with a `Status: Draft` line, a link to `requirements.md`, and these sections: Stack and commands, Architecture (with the diagram and a project/responsibility table), Decisions (table: #, decision, options, why, consequences), Phases (a summary table of number, title, status, demo command, note, then one short block per phase with goal, scope, out of scope, tests, acceptance criteria covered, demo command with expected output, and estimate), Parallel streams, Risks, Known limitations. Set every phase status to `Not started`.

8. **Ask for review.** Summarize phases in a compact table (number, goal, demo command) and ask the user to approve or adjust. When approved, set the plan status to `Approved`. Next step: `/implement-phase 1`.
