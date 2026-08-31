# Changelog

All notable changes to this project are documented here.

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
