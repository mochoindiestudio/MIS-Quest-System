---
name: push-it
description: Commit outstanding work, update CHANGELOG.md, bump the package version, and push to remote. Use when the user says "push it" or asks to commit+release/publish progress on this project.
---

# push-it

A repeatable release routine for this project, adapted from the MIS Dialog Editor project's `push-it`
skill for a UPM package repo (version lives in `package.json`, not `ProjectSettings.asset`). Run these
steps in order, every time this skill is invoked. Do not skip steps or silently reinterpret them —
this touches shared remote state.

## 1. Determine commit scope — ASK, don't guess

Run `git status --porcelain` (and `git diff --cached --stat` / `git diff --stat` if useful) to see
what's staged vs. unstaged/untracked. **Always ask the user** whether to commit:

- **Current work** — only the changes from this working session, or
- **All work** — every outstanding change in the working tree, including anything untracked/modified
  that predates this session.

Use AskUserQuestion. Do not assume — even when the scope looks obvious from `git status`. Note this
repo has pre-existing uncommitted working-tree changes from before the package work began
(deleted `Assets/Editor/HubForceResolve.cs`, modified `Assets/Settings/*.asset`,
`ProjectSettings/*.asset`, `Packages/manifest.json`, `packages-lock.json`) — do not sweep those into
a "current work" commit unless the user says so.

Stage accordingly (`git add <specific files>` for "current work", or all outstanding changes for
"all work" — still avoid `git add -A`/`git add .` if that would sweep up something unrelated; check
`git status` first).

## 2. Read the current version

Version lives in the package's `package.json`
(`Packages/com.mochoindiestudio.quest-system/package.json`) as `"version": "X.Y.Z"` — this is the UPM
package version, not a Unity `bundleVersion` (there is no game/player build here).

- If the package doesn't exist yet or this is the very first release, the version becomes **`0.1.0`**
  unless the user says otherwise.
- Otherwise, read the current `X.Y.Z`.

## 3. Classify the change and bump the version

**Never bump major** — that only happens on the user's explicit, separate request, never as part of
this routine.

Look at what's being committed (the diff) and classify:

- **Patch** (`Z += 1`) — small changes, bug fixes, tweaks, docs-only, tuning values.
- **Minor** (`Y += 1`, `Z` resets to `0`) — new features, new objective/condition types, new editor
  tooling, or otherwise substantial additions.

If the classification genuinely isn't clear-cut, ask the user rather than guessing.

Update `"version"` in `package.json` to the new value.

## 4. Update CHANGELOG.md

`CHANGELOG.md` lives at the **package root**
(`Packages/com.mochoindiestudio.quest-system/CHANGELOG.md`) so Unity's Package Manager surfaces it as
the "Changelog" tab for Git-installed consumers. It follows
[Keep a Changelog](https://keepachangelog.com) conventions:

```markdown
# Changelog

All notable changes to this project are documented here.

## [X.Y.Z] - YYYY-MM-DD

- ...
```

Prepend a new `## [X.Y.Z] - YYYY-MM-DD` section (today's date) above prior entries, summarizing what's
in this commit as concise bullet points (group under `### Added` / `### Changed` / `### Fixed` only if
there's enough content to warrant it — otherwise a flat bullet list is fine for small releases).

## 5. Commit

Stage `package.json` and `CHANGELOG.md` alongside the work from step 1. Write a commit message with a
concise summary line (mention the version, e.g. `v0.1.0: <summary>`) — follow the repo's existing
commit-message conventions once some exist. Follow the standard git-commit workflow (review `git diff`,
heredoc-formatted message, no `--no-verify`, no amending).

## 6. Push, with LFS

Confirm a remote exists first (`git remote -v`) — `origin` is `mochoindiestudio/MIS-Quest-System` on
GitHub. Work happens on `development` per the project's branching workflow — push there (`git push`,
add `-u origin development` only if tracking isn't already set up), never push to `main` unless the
user explicitly asks for a PR/merge.

If `git lfs status` shows anything unexpected, surface it before pushing rather than pushing blind.

Pushing is the one irreversible, externally-visible step here — if anything in steps 1-5 felt
ambiguous and wasn't resolved by asking, pause before this step rather than pushing uncertain state.
