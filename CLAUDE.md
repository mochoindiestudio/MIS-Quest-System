# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project state

`MIS Quest System` is a **Unity 6.3 (6000.3.19f1)** / URP-2D project whose entire deliverable is an
**installable UPM package** — a node-graph quest editor + UI-agnostic runtime, reusable across
several Mocho Indie Studio games and a companion to the **MIS Dialog System** package. The game
project in this repo exists only to exercise/demo the package. This is non-negotiable: nothing in the
runtime API, editor tooling, or data assets may assume it lives inside one specific game's `Assets/`.

**v0.1.0 implemented** (2026-08-31), compiling clean: `Runtime/` (data layer + `QuestLog` engine +
signal bus + snapshots + optional `QuestLogHost`) and `Editor/` (6 scripts —
`QuestGraphEditorWindow` GraphView, opens on double-clicking a `Quest`; `QuestRootNodeView` +
`ObjectiveNodeView` with `SerializedObject`-bound `PropertyField`s; `QuestAssetIcons`). **Not built:**
the QuestList prerequisite-graph view (QuestList uses the default Inspector), EditMode tests. Read
**`docs/quest-system-spec.md`** first — it has the model, `QuestLog` API, and every as-built note.

- Package will live at `Packages/com.mochoindiestudio.quest-system/` (`Runtime/` + `Editor/` asmdefs,
  namespace `MochoIndieStudio.QuestSystem`), laid out like the Dialog System's
  `com.mochoindiestudio.node-dialog-system`. **No Assets-side copy of package scripts.**
- Core model: a **Quest** is a staged list of **Objectives**; standalone `Quest` assets referenced by
  `QuestList` assets. Completion driven by signals on the shared **MIS Signals** bus
  (`com.mochoindiestudio.signals`) — `QuestLog` is an `ISignalListener` subscribed to
  `MisSignals.Report(eventId, payload)`, the same `EventId`+`Payload` shape the Dialog and Inventory
  packages use (**v0.4.0**; there is no more `QuestSignals` type). Rewards are the game's job (its
  `OnQuestCompleted` handler).

Key facts:
- Input: **new Input System only** (`activeInputHandler: 1`). Action asset at `Assets/InputSystem_Actions.inputactions`. Do not use `UnityEngine.Input`.
- Rendering: URP 2D. Renderer/pipeline assets under `Assets/Settings/`.
- Main scene: `Assets/Scenes/SampleScene.unity`.
- Version: `bundleVersion 0.1.0`, company `DefaultCompany` (unset).

## Working in this repo

This project is driven through the **Unity CLI** (experimental) talking to the running Editor via
the `com.unity.pipeline` package — **not** an MCP server. Full setup notes, verified environment
status, and command reference: **`docs/unity-cli.md`**. Invoke the `unity-cli` skill for Editor work.

- **PATH (Bash tool):** `export PATH="$PATH:/c/Users/crist/AppData/Local/Unity/bin"`
- **Check connection first:** `unity status` (want State `ready`); `unity list` for the 143 Editor commands.
- **Drive the Editor:** `unity command <name> [args]`, `unity command eval '<C#>'`. Do NOT hand-edit
  `.unity`/`.prefab`/`.asset`/`.meta` YAML while `unity status` shows a reachable Editor.
- **Safe Mode:** compile errors → Editor drops to Safe Mode, pipeline won't connect. Confirm with
  `unity pipeline list`, then fix the C# and restart Unity.
- **Tests:** `com.unity.test-framework` 1.6.0. `unity test "E:/Mocho Indie Studio/MIS Quest System" --mode EditMode --report-format junit --output results.xml` (exit 6 = failures). PlayMode/EditMode tests need their own asmdef referencing the code-under-test.
- **Never** hand-edit files under `Library/`, `Temp/`, `Logs/`, `obj/`, or the generated `*.csproj`/`*.slnx` (all git-ignored). `UserSettings/` is also ignored.

## Conventions for new code

- Package code lives under `Packages/com.mochoindiestudio.quest-system/{Runtime,Editor}/`, each with
  its own asmdef (Editor references Runtime). Not under `Assets/Scripts/`.
- Namespace `MochoIndieStudio.QuestSystem` / `.Editor`. Studio C#/Unity guidelines apply (composition
  over inheritance, no static mutable state, no LINQ/GC in hot paths, `[SerializeField]` not public
  fields, documented code, no magic numbers, inspector knobs for tunables/resources).
- Every asset/script needs its committed `.meta` file — create files through Unity / `unity command`
  so GUIDs are generated, rather than writing them raw. `CHANGELOG.md` and `LICENSE.md` at the
  package root need committed metas too (git-installed packages are immutable folders).
- Scene and prefab wiring should go through `unity command` or the Editor, not manual YAML edits.
- Don't self-test by running the project/editor; a compile check via `unity command` is fine, visual
  verification is the user's job.
- Work on the `development` branch (tracks `origin/development`); PR to `main` only when explicitly
  asked. Use the `push-it` skill for the commit / version-bump / changelog / push routine.
