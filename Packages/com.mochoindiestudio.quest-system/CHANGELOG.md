# Changelog

All notable changes to this project are documented here.

## [0.4.0] - 2026-09-03

Migration to the shared **MIS Signals** bus (`com.mochoindiestudio.signals`), so the Quest, Dialog
and Inventory packages integrate through a game without referencing each other's assemblies.

### Changed

- **BREAKING — `QuestSignals` is removed.** The package no longer defines its own signal static.
  `QuestLog` now implements `MochoIndieStudio.Signals.ISignalListener` and subscribes itself to
  `MisSignals` on construction (unsubscribes on `Dispose`). Replace `QuestSignals.Report(...)` calls
  with `MisSignals.Report(...)` (`using MochoIndieStudio.Signals;`). `QuestLog.Report(...)` stays
  public for targeting a single log directly. No migration shim — the package has no external
  consumers yet.
- `package.json` now declares a dependency on `com.mochoindiestudio.signals` (>= 0.1.0); the runtime
  asmdef references `MochoIndieStudio.Signals`.

### Notes

- The "Water from the Well" demo (`Assets/QuestSystemDemo/`, not part of the shipped package) and all
  docs are updated to `MisSignals.Report`.

## [0.3.0] - 2026-09-02

### Added

- `QuestLog.OnObjectiveActivated` event — fires when an objective becomes `Active` (its stage is
  reached, or the quest just started), before any same-frame completion check. Lets a UI react to a
  new objective without polling.

### Notes

- The repo now carries a full playable demo under `Assets/QuestSystemDemo/` ("Water from the Well" —
  tilemap mini-game exercising the quest system end to end). It is **not** part of the shipped
  package. Build it with `Tools ▸ Quest Demo ▸ Build Scene`; see `docs/quest-demo-scene.md`.

## [0.2.0] - 2026-09-02

Object-model simplification. **Breaking** — the package has no external consumers yet, so no
migration path is provided; re-author any local quest assets.

### Changed

- **One unified `QuestCondition` tree.** `ObjectiveCompletion` and `ConditionCompletion` are removed;
  `SignalCompletion` → `SignalCondition`, `ManualCompletion` → `ManualCondition`. The same condition
  type now drives objective *complete-when*, objective *fail-when* and a quest's advanced unlock.
  `QuestCondition.Evaluate` takes `in QuestConditionContext` (was `IQuestContext`); the counting hook
  is `HandleSignal`, the UI target is `GetProgressTarget`.
- **`Objective`**: `Completion` → `CompleteWhen`; the `FailConditions` list → a single optional
  `FailWhen` condition (use a `CompositeCondition` for several).
- **`Quest`**: `Prerequisites` and `FailConditions` are removed. Prerequisites are now `UnlockedBy`
  (a `List<Quest>`) + `UnlockMode` (`All` / `Any`) + an optional `AdvancedUnlock` condition. A quest
  fails only when a required objective fails or its time limit expires.
- `QuestStateCondition` references a `Quest` asset directly instead of a GUID string.
- `QuestFailReason` drops `FailCondition`.
- `Quest.OnValidate` backfills a null `CompleteWhen` with a `SignalCondition`.

### Added

- **Quest List graph window** (`QuestListGraphEditorWindow`) — opens on double-clicking a
  `QuestList`. One node per quest; drag from a quest's "Unlocks" port to another's "Requires" port to
  author a prerequisite link (written into `UnlockedBy`). Drop `Quest` assets onto the canvas to add
  them; "New Quest" creates one beside the list; prerequisite cycles are refused; double-clicking a
  node opens that quest's own graph.
- `PrerequisiteMode` enum; `IQuestGraphCanvas` editor interface shared by both graph views.
- `QuestList` stores per-quest graph node positions.
- Repo-root `ROADMAP.md` — single phased plan.

## [0.1.0] - 2026-08-31

Initial release of the `com.mochoindiestudio.quest-system` package ("MIS Quest System"): a
node-graph quest editor and UI-agnostic runtime, companion to the MIS Dialog System.

### Added

- **Data layer:** `Quest` and `QuestList` ScriptableObjects; `Objective` (inline, staged, with
  required / hidden flags); polymorphic `ObjectiveCompletion` (`SignalCompletion` /
  `ConditionCompletion` / `ManualCompletion`); polymorphic `QuestCondition` (`QuestStateCondition`,
  `CompositeCondition`) used for prerequisites, fail conditions and condition-completion. Stable
  GUID ids on quests and objectives.
- **Runtime engine:** `QuestLog` (plain C# class) drives the quest / objective state machines,
  stage progression, signal counting, prerequisite and fail-condition evaluation, time limits and
  bound predicates, and raises nine lifecycle events. `QuestSignals.Report(eventId, payload, amount)`
  is the global entry point (event-id + payload shape matches the Dialog System's `DialogEventTrigger`).
  `CaptureState()` / `RestoreState()` give a `JsonUtility`-friendly save/load snapshot. Optional
  `QuestLogHost` MonoBehaviour owns a log and pumps `Tick`.
- **Editor:** `QuestGraphEditorWindow` — a dedicated GraphView window that opens on double-clicking a
  `Quest` asset, showing the quest's root node plus one node per objective. Node fields are
  `SerializedObject`-bound `PropertyField`s (Undo-aware, Inspector-synced, with Unity's built-in
  `[SerializeReference]` type dropdowns). Toolbar with Add Objective and Snap to Grid. Custom
  Project-window icons for `Quest` / `QuestList` via `MonoImporter.SetIcon`.
- `Create > MIS Quest System > Quest` and `> Quest List` menu items.
