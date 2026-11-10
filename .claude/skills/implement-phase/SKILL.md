---
name: implement-phase
description: Implement one phase, or a range of phases, from docs/plan.md end to end - code plus meaningful tests, build, test, run the demo command, update the phase status, prepare each phase for the user's review, and commit it once the user says so. Use when the user says to build, implement, or start a phase (for example "implement phase 2", "implement phase 2-3", or "next phase").
argument-hint: "[phase-number | first-last]"
---

# Implement phase $ARGUMENTS

## Which phases

- **No argument:** use the first phase in `docs/plan.md` whose status is not `Done`, and say which one you picked.
- **One number** (`2`): that phase.
- **A range** (`2-3`, or a list such as `2,3`): run each phase in ascending order through steps 1-5, one at a time. Each phase gets its own verification and status update before the next one starts, but don't stop for review between phases. After the last phase in the range, do one review (step 6) covering the whole range, then one commit for the range (step 7). If the user explicitly said to commit without review (for example "implement phase 2-3, commit without review"), instead commit each phase on its own after it passes verification and go straight on to the next. If any phase fails, stop there, do not start later phases, and prepare what is finished for review.

## Preconditions (check, do not assume)

- `docs/plan.md` status is `Approved`. If it is `Draft`, stop and ask the user to approve the plan.
- Every phase before the first requested one is `Done`. If not, stop and say which one is outstanding.
- `git status` shows no unexpected uncommitted changes in `src/` or `tests/`. If there are some, ask before continuing.
- Run the build and default test commands recorded in `docs/plan.md` once before starting (skip for phase 1, which creates the projects). If the baseline is red, fix or report it before adding new work.

## Steps (per phase)

1. **Read the phase** in `docs/plan.md`: goal, scope, tests, demo command, acceptance criteria it covers. Re-read the relevant parts of `docs/requirements.md` and `CLAUDE.md`.

2. **Mark it `In progress`** in `docs/plan.md`.

3. **Build it in small steps.** Write tests alongside the code for behavior that warrants them, as defined in the Testing section of `CLAUDE.md` (test first when the behavior is clear). Follow the stack decisions in `docs/plan.md` and the file ownership rules in `CLAUDE.md`. Keep to the phase's scope; if you find work that belongs to a later phase, note it rather than doing it.
   - **Phase 1** creates the project(s): the app and test projects in the chosen stack, shared build settings (warnings as errors where supported), configuration loading in the order given in `CLAUDE.md`, logging, an entry point that runs, and at least one meaningful passing test. Keep the structure stable afterwards, because documentation and pipeline sessions build on it.

4. **Verify for real**, using the commands recorded in `docs/plan.md` (for example `dotnet build` / `dotnet test`, `npm run build` / `npm test`, or `pytest`, depending on the stack):
   - The build succeeds with no warnings.
   - The default test command (Live tests excluded) is all green.
   - If the phase added Live tests and `secrets.json` has the needed values, run them too and report the result.
   - Run the phase's demo command and check the output matches the expected output in the plan.
   - Check that the phase's acceptance criteria are covered by meaningful tests and add any that are missing. For .NET projects, the `dotnet-test` plugin skills (for example test gap analysis) help here.

5. **Update `docs/plan.md`**: phase status `Done`, a one-line note (what was built, test count), and any deviations from the plan or new known limitations.

6. **Prepare for review, then stop** (Review before commit in `CLAUDE.md`). For a range, do this once after the last phase and report each phase. Report in a few lines: what was built, tests added and passing, the demo command and its actual output (with any secret values masked), deviations, the changed files, the proposed commit message (see step 7), and the exact paths to commit. Don't stage or commit. If the user leaves review comments (often through `/local-comments`), fix them, re-run the build and tests, and prepare for review again. Skip this stop only if the user explicitly said to commit without review.

7. **Commit when the user says so.** Confirm `secrets.json` is not staged, then commit `src/`, `tests/`, build files, and `docs/plan.md` by explicit path, so files from parallel sessions aren't swept in: `git add -- <those paths>`, then `git commit -m "phase-N: <phase title>" -- <those paths>`. A reviewed range is one commit, because its phases' changes can't be reliably split apart after the fact: use `phase-2-3: <phase 2 title>; <phase 3 title>` (the first and last phase numbers). After the last requested phase, do not start another until the user says so.

If anything fails and cannot be fixed within the phase's scope, stop, leave the phase `In progress`, and report exactly what failed with the output.
