---
name: deploy-pipeline
description: Create a GitHub Actions pipeline that builds, tests, and publishes the application to Azure, whatever its shape (web app, API, function app, static site, container, or CLI), and set the GitHub repository secrets the pipeline needs. Writes .github/workflows/ and docs/deployment.md. Use when the user asks for a pipeline, CI/CD, GitHub Actions, a deploy workflow, or to publish the app to Azure. Safe to run in a parallel session once phase 1 has created the project(s).
argument-hint: "[target resource name | ci-only]"
---

# Deployment pipeline

Target: $ARGUMENTS (if empty, choose it in step 2)

**CI-only mode** (`ci-only`, or chosen in step 2): build and test on every push and pull request, plus a job that uploads the build output (published folder, package, or binary) as a workflow artifact. There's no Azure target, no Azure authentication, and no repository secrets, so skip steps 3 and 5. This is the usual choice for a CLI or library that has no hosting resource.

You may write only `.github/workflows/`, `docs/deployment.md`, and GitHub repository secrets. Never edit `src/`, `tests/`, or other docs. Follow the Accuracy and integrity section of `CLAUDE.md` strictly: action names, action versions, inputs, and Azure CLI flags change over time, so check the current official documentation (the action's README on GitHub, Microsoft Learn, or `az <command> --help`) before using them, and say which source you checked.

## Preconditions

- Phase 1 in `docs/plan.md` is `Done`, so the project structure and the build, test, and run commands exist. If not, stop and say so.
- `gh auth status` succeeds and `git remote get-url origin` points to a GitHub repository. If either fails, report it and stop. You can still draft the workflow file if the user asks.

## Steps

1. **Work out the application's shape** from `docs/plan.md` and the code: language and toolchain, build/test commands, the deployable output (for example a published folder, a static `dist/`, a container image, or a CLI binary or package), and how it runs (HTTP server, functions, static files, command line).

2. **Choose the Azure target.** Read `docs/environment.md` for the resources that exist. Match the shape to a resource, for example a web app or API to App Service, functions to a Function App, a static site to Static Web Apps or Storage static hosting, or a container to Container Apps. If more than one fits, or **nothing obviously fits (common for a CLI)**, ask the user where it should be published, with a proposed default; offer CI-only mode as one of the choices (and as the default when nothing fits). Never create, change, or delete Azure resources unless the user explicitly asks.

3. **Choose how the workflow authenticates**, and confirm with the user:
   - **OIDC (federated credential)** is generally the preferred option for `azure/login`, but it needs an Entra ID app registration or managed identity with a federated credential and a role assignment. Those are Azure changes: state exactly what would be created and get an explicit yes first. If the user declines, use a resource-scoped credential.
   - **Resource-scoped credential** that already exists, for example an App Service or Function App publish profile, or a Static Web Apps deployment token. Reading it is read-only, but it is a secret: never print it. Publish profiles depend on SCM basic-auth publishing credentials being enabled on the app; check that setting rather than assuming it.

4. **Write the workflow** in `.github/workflows/deploy.yml` (or a clearer name for the target):
   - Triggers: push to the default branch, and `workflow_dispatch`.
   - A build-and-test job using the commands from `docs/plan.md`. Exclude Live tests, because the pipeline has no `secrets.json`.
   - A deploy job that depends on build-and-test and publishes the build output to the target.
   - Least-privilege `permissions:` (add `id-token: write` only for OIDC), pinned action versions taken from current docs, and secrets referenced only as `${{ secrets.NAME }}`.
   - Settings the app needs at runtime belong in the Azure resource's configuration, not in the workflow. List any that are missing for the user rather than changing Azure settings yourself.
   - The hosted app calls Foundry with Microsoft Entra ID (see `CLAUDE.md`), so it needs a managed identity with the **Foundry User** role on the Foundry project, and app settings for `Foundry__ProjectEndpoint` and `Foundry__DefaultDeployment` (or the stack's equivalent). Check what exists with read-only commands (for example `az webapp identity show`). Enabling an identity or assigning a role is an Azure change: list exactly what's needed in `docs/deployment.md`, and only make the change if the user explicitly says yes.

5. **Set the GitHub secrets.** The user has given permission for this skill to write repository secrets.
   - Use `gh secret set <NAME> --repo <owner>/<repo>` and pass the value on **standard input** (for example piped from the command that reads it), never as a command-line argument and never echoed to the output.
   - Afterwards, run `gh secret list --repo <owner>/<repo>` (it shows names only) to confirm each one exists.
   - If setting a secret fails, do not retry with a less safe method. Report the error and give the user the exact commands to run themselves, with the value shown only as a placeholder or as the command that produces it (for example `az webapp deployment list-publishing-profiles --name <app> --resource-group <rg> --xml | gh secret set AZURE_WEBAPP_PUBLISH_PROFILE --repo <owner>/<repo>`).

6. **Write `docs/deployment.md`**: the target resource, the authentication method, what triggers the workflow, the secret names (never values) and where each comes from, runtime settings the app expects on the resource, and how to run the workflow by hand.

7. **Verify what you can.** Check that the workflow YAML parses and that every action version you used exists. Do not push or trigger a run without the user's approval. If the user approves a run, watch it with `gh run watch` and report the actual result. Otherwise, say plainly that the pipeline has not been run.

8. **Report and prepare for review**, following Review before commit in `CLAUDE.md`: the target, the authentication method, the secrets set (names only) and whether each was confirmed, the files written, what is still unverified, and the proposed commit message. Don't commit until the user says so.

9. **Commit when the user says so**, only your files, by explicit path, because the implementation session may have files staged in the same working folder: `git add -- .github/workflows docs/deployment.md`, then `git commit -m "pipeline: <summary>" -- .github/workflows docs/deployment.md`. Confirm `secrets.json` is not included.
