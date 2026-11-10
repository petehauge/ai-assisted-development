# Buildout session: prompts

The prompts used in the main buildout session on 2026-11-10 (clarify, plan, and phases 1–4 of the Text-to-SQL app), in order, verbatim. Commands run outside the conversation and answers given through next-step buttons are included and marked as such. The `/azure-secrets` and `/deploy-pipeline` sessions ran in parallel and are not included; see `deploypipeline-prompts.md` for the pipeline session.

1. `/clarify` with the prompt:

   > Build a text to sql system - that accepts question in English and generates and executes sql against selected database and returns the answer
   >
   > We are building an API with a website - use .net and blazor
   > create a sample database to start with

2. Answers to the clarifying questions:

   > For the database engine let's use Azure SQL.
   > For now for the database, we will leave it at a built in test database.
   > On what the answer contains, it should be the generated sequel and maybe the top fifty rows.
   > For model calls, let's just do model calls for the AI behavior and not do not build an agent.
   > If SQL fails, we just show the error to the user.
   > Let's enable all of the SQL types like Select and Drop and Delete for now.
   > And what you have to go for the model is fine, so the table names columns types keys.

3. > Confirm

4. `/plan-phases`

5. > Approve

6. > Commit both

7. `/compact` (command run directly, not a prompt)

8. > please get starting on phase 1

9. > Commit the working tree changes with a sensible message.

   (Commit button in the next-step box, answering "Commit phase 1 with the proposed message and paths?")

10. `/implement-phase 2-4`

11. > Commit the working tree changes with a sensible message.

    (Commit button in the next-step box, answering "Commit phases 2–4 with the proposed message and paths?")

12. > please export all the prompts I used to a file, "/AI/buildout-prompts.md"
