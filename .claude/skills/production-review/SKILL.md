---
name: production-review
description: Production readiness review of the codebase across security, reliability, performance, scalability, maintainability, test quality, observability, compliance and privacy, configuration and secrets, operability, and cost - run as parallel reviewers and merged into one self-contained HTML report at reports/production-review.html. Use when the user asks for a production review, readiness review, quality report, or an HTML review report.
argument-hint: "[scope: path or 'changes'] [quick]"
---

# Production readiness review

Arguments: `$ARGUMENTS`

- Scope: a path (default: the whole repo, `src/` and `tests/`), or `changes` for the diff since the last review or `main`.
- `quick`: skip the `claude-security` full scan and use the built-in security reviewer instead.

This skill is **read-only** with respect to code. It writes only to `reports/`.

## Steps

1. **Gather context.** Read `docs/requirements.md`, `docs/plan.md`, and `CLAUDE.md` so findings are judged against the stated requirements, boundaries, and accepted limitations. Run the build and default test commands recorded in `docs/plan.md` (Live tests excluded) and record the results (pass/fail counts, warnings) for the report header.

2. **Run reviewers in parallel.** Launch all of the following in a single message so they run concurrently. Give each one the scope, the relevant section of `areas.md` (next to this file), and the finding schema below, and tell it to return only findings it can support with a file and line.

   | Area | Reviewer |
   |---|---|
   | Security | `claude-security` plugin scan of the scope (or a general-purpose subagent applying the Security checklist when `quick`) |
   | Reliability and error handling | `pr-review-toolkit` `silent-failure-hunter` agent, plus the Reliability checklist |
   | Test quality and coverage | `pr-review-toolkit` `pr-test-analyzer` agent, plus the Test quality checklist |
   | Maintainability and code quality | `pr-review-toolkit` `code-reviewer` and `type-design-analyzer` agents, plus the Maintainability checklist |
   | Performance | general-purpose subagent with the Performance checklist |
   | Scalability | general-purpose subagent with the Scalability checklist |
   | Observability | general-purpose subagent with the Observability checklist |
   | Compliance and privacy | general-purpose subagent with the Compliance checklist |
   | Configuration and secrets | general-purpose subagent with the Configuration checklist |
   | Operability | general-purpose subagent with the Operability checklist |
   | Cost | general-purpose subagent with the Cost checklist |

   Finding schema (each reviewer returns a JSON array):

   ```json
   {
     "area": "Performance",
     "severity": "Critical | High | Medium | Low | Info",
     "confidence": "High | Medium",
     "title": "Short statement of the problem",
     "file": "src/Project/File.cs",
     "line": 42,
     "evidence": "What in the code shows this (no secret values)",
     "impact": "What goes wrong in production and when",
     "recommendation": "Concrete fix",
     "effort": "S | M | L"
   }
   ```

   Each reviewer also returns a one-line `summary` and a `rating` for its area: `Ready`, `Needs work`, or `Not ready`.

3. **Verify and merge.** Drop findings without a file reference or with low confidence. Spot-check every Critical and High finding yourself by reading the cited code; downgrade or drop any you cannot confirm. Merge duplicates reported by more than one reviewer (keep the clearer one, note both areas). Do not report accepted limitations from `docs/plan.md` as defects; list them separately.

4. **Write the report.** Copy `report-template.html` (next to this file) to `reports/production-review.html` and replace the JSON inside `<script id="review-data" type="application/json">` with the merged data:

   ```json
   {
     "project": "name",
     "generatedAt": "ISO-8601 UTC",
     "commit": "short sha",
     "scope": "src/, tests/",
     "build": { "status": "Pass | Fail", "warnings": 0 },
     "tests": { "passed": 0, "failed": 0, "skipped": 0 },
     "overall": "Ready | Needs work | Not ready",
     "summary": "Two or three sentences for an executive reader.",
     "areas": [ { "name": "Security", "rating": "Needs work", "summary": "..." } ],
     "findings": [ /* finding objects as above */ ],
     "acceptedLimitations": [ "..." ]
   }
   ```

   Escape `<` as `<` inside JSON strings so the script block cannot be closed early. Never include secret values in evidence.

5. **Report back** in chat: overall rating, counts by severity, the top three to five findings with file:line, and the report path. Offer to fix the Critical and High findings in the implementation session.
