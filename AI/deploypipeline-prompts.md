# Deploy pipeline session: prompts

The prompts used in the `/deploy-pipeline` session on 2026-11-10, in order, verbatim. Answers given through question dialogs and next-step buttons are included and marked as such.

1. `/deploy-pipeline`

2. Answers to the setup questions (dialog):
   - Target: **aiasst-web-m4gvse (Recommended)**
   - Authentication: **Publish profile (Recommended)**

3. > you can check in the pipeline & run it whenever you're ready

4. > Please handle any updates that are needed to make it work end to end, and run it to confirm it works

5. > Deploy working tree now

   (next-step button, answering "Should I wait for phases 2–3 to be committed, then run and verify the deploy?")

6. > Please update the pipeline so that it deploys on every time the code checked into Main.

7. > Commit it

   (next-step button, answering "Commit the docs/deployment.md update?")

8. > Push and watch it

   (next-step button, answering "Push main to trigger the pipeline deploy?")

9. > please export all the prompts I used to a file, "/AI/deploypipeline-prompts.md"
