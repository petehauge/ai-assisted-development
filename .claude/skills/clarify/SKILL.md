---
name: clarify
description: Turn the user's product prompt into clarifying questions, agreed scope, boundaries, and acceptance criteria, written to docs/requirements.md. Use at the start of a new build, when the user pastes a prompt like "build a tool that...", or asks to clarify requirements or scope.
argument-hint: "[the prompt to build]"
---

# Clarify requirements

Prompt: $ARGUMENTS

If no prompt was given, ask for it. Timebox: this whole step should take about 5–8 minutes of the session.

## Steps

1. **Read context.** Read `docs/environment.md` if it exists (which Azure resources and models are available) and `CLAUDE.md`. Do not explore the web or write code.

2. **Restate the problem** in two or three sentences: who uses it, what goes in, what comes out.

3. **Ask clarifying questions** in one message, at most 10, grouped under these headings. For every question, give your **proposed default** so the user can answer "go with defaults" or override only a few:
   - Users and usage: who runs it, how often, interactive or CI.
   - Inputs and outputs: input sources and sizes, output formats, exit codes.
   - AI behavior: which Foundry model deployment from `docs/environment.md`, whether it needs a Foundry agent or a plain model call (both through the Foundry projects API), what the model decides vs what code decides, how results are validated.
   - Boundaries: what the tool must never do (for example send secrets to the model, modify the target, exceed a cost or token budget, run longer than N minutes).
   - Quality bar: performance targets, error handling expectations, logging.
   - Out of scope for this session.

   Prefer questions whose answers change the design. Skip anything with an obvious default.

4. **Wait for answers.** Do not proceed until the user replies.

5. **Write `docs/requirements.md`** with a `Status: Draft` line, the source prompt, and these sections: Problem, Users and usage, In scope, Out of scope, Inputs and outputs, AI behavior, Boundaries, Quality bar, Assumptions, Acceptance criteria, Open questions. Keep it short; mark assumptions explicitly as assumptions. Acceptance criteria get IDs (AC1, AC2, …) and must be testable statements ("Given X, when Y, then Z"), because phases and tests will be derived from them. Set the status to `Confirmed` once the user confirms.

6. **Confirm.** Show a short summary (scope in, scope out, top three boundaries, acceptance criteria count) and ask the user to confirm or adjust. Next step after confirmation: `/plan-phases`.
