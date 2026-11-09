---
name: local-comments
description: 'Handle the user''s local code-review comments stored in `local-comments.json` (written by the VS Code "local-comments" extension). Use this whenever the user says "please check comments", "check my comments", "address my review comments", "look at local comments", or otherwise refers to review comments they left in the editor — in any repository. Covers finding the file, its format, how to resolve/answer/ask about each comment, and the rule that comments are deleted only after the user approves.'
---

# Local review comments

The user reviews changes in their editor with the VS Code extension [marcelrsoub/local-comments](https://github.com/marcelrsoub/local-comments) instead of on GitHub. The extension writes the comments to a JSON file, and that file is their review feedback. When they ask you to check comments, handle **every** comment in it.

## 1. Find the file

- The filename is always `local-comments.json`.
- It is usually in the **root of the current repository**. Get the root with `git rev-parse --show-toplevel` and look there first.
- If it's not there, search the working tree for `local-comments.json` (skip `node_modules`, `bin`, `obj`, `.git`). If you find none, or more than one, tell the user and ask. Don't guess.
- If the file exists but holds no comments (`{}` or only empty arrays), say so and stop.

## 2. Read the format

The file is a JSON object keyed by **absolute file path**, usually with a lower-case drive letter such as `c:\sources\...`. Each value is an array of comments:

```json
{
  "c:\\sources\\repo\\src\\Foo.cs": [
    {
      "id": "…",
      "text": "the review comment",
      "timestamp": 1730000000000,
      "range": { "startLine": 15, "startCharacter": 0, "endLine": 15, "endCharacter": 9007199254740991, "selectedText": "optional" }
    }
  ]
}
```

- Lines are **0-based**: `startLine` 15 is editor line 16. When you cite a location to the user, add 1.
- An `endCharacter` of `9007199254740991` means the whole line.
- Line numbers go stale once the file has been edited after the comment was written. Find the code by `selectedText` or nearby content too, and if the range no longer matches anything sensible, say so.
- Path keys may not match the on-disk casing exactly. Match paths case-insensitively on Windows.

## 3. Handle each comment

For each comment, read the code it points at, then do exactly one of these:

- **Resolve.** Make the change it asks for, following the repository's own rules (CLAUDE.md, docs, tests, UI verification and so on).
- **Respond.** If it's a question or remark rather than a change request, answer it.
- **Ask.** If the intent is unclear, ask a specific question. Don't assume.

Notes:
- The extension has **no replies, threads or resolved flag**. All replies go in chat.
- Comments that ask for outward-facing changes, such as cloud resources, app settings or anything outside the repo, are not plain code edits. State exactly what will change and confirm before doing it.
- Fixing comments never authorizes a commit or push. Follow the repository's and the user's commit rules as usual.

## 4. Report back

In chat, list every comment with:
- its location (`path:line`, 1-based) and a short quote of the comment text,
- what you did: the fix, the answer, or the question you need answered,
- anything you couldn't verify.

## 5. Delete only approved comments

- **Never remove a comment just because you addressed it.**
- After the report, the user says which items are good. Delete only those, by `id`, from `local-comments.json`. Leave every other comment, and every other field, untouched.
- If a file key's array becomes empty, removing the key is fine.
- Keep the JSON valid. Preserve the existing formatting and indentation style, and re-read the file right before editing in case the extension changed it meanwhile.

## 6. Keep it out of git

`local-comments.json` is personal review state and must never be committed. Check `git check-ignore local-comments.json`. If it isn't ignored, tell the user and offer to add it to `.gitignore`. Don't add it without asking, since that changes a tracked file.
