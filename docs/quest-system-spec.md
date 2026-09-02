# MIS Quest System — design spec

Status (2026-09-02): **v0.2.0 — object-model simplification** on top of the v0.1.0 engine. One
unified `QuestCondition` tree (no separate `ObjectiveCompletion`); failure is objective-level only;
quest prerequisites are a `Quest` list authored as edges in a new **Quest List graph window**.
Roadmap and phase status live in `ROADMAP.md` at the repo root. EditMode tests still deferred (user
tests manually). Compiled design below; deviations as built are noted inline with "**As built:**";
v0.2.0 changes are marked "**v0.2.0:**".
Companion package to **MIS Dialog System** (`com.mochoindiestudio.node-dialog-system`); the two are
independent packages but follow the same conventions and event style so a game can use both.

## 1. Purpose & constraints

- Ships as a **standalone installable UPM package** — reusable across multiple game projects, never
  built inside one game's `Assets/`. Non-negotiable (same rule as the Dialog System).
- **UI-agnostic.** The package exposes data + `event Action` hooks only. It never renders quest UI
  (journals, trackers, toasts) — the consuming game brings its own UI, exactly like `DialogRunner`.
- **Deterministic.** Same quest definitions + same sequence of signal reports and ticks → same
  outcome.
- Follows the studio C#/Unity guidelines: `MochoIndieStudio.QuestSystem` namespace, composition over
  inheritance, no static mutable state (the one static is the stateless `QuestSignals` forwarder), no
  LINQ / GC in hot paths, `[SerializeField]` not public fields, documented code, no magic numbers.

## 2. Package identity

| | |
|---|---|
| Folder | `Packages/com.mochoindiestudio.quest-system/` |
| Display name | `MIS Quest System` |
| Runtime namespace | `MochoIndieStudio.QuestSystem` (asmdef `MochoIndieStudio.QuestSystem`) |
| Editor namespace | `MochoIndieStudio.QuestSystem.Editor` (asmdef references Runtime) |
| Layout | `package.json`, `README.md`, `CHANGELOG.md` + `.meta`, `LICENSE.md` (MIT) + `.meta`, `Runtime/`, `Editor/`, `Samples~/` (empty for now) |
| Consumed via | git URL in the game's `Packages/manifest.json` |

## 3. Domain model

### Nomenclature (fixed)

- **Quest** — a titled unit of work; it *is* an ordered/staged list of **Objectives**, plus the
  quests that unlock it, an optional extra unlock condition, a time limit, a repeatable flag and
  events.
- **Objective** — one line item inside a Quest ("press WASD to move", "collect 10 pelts", "talk to
  Giorgio"). Has a *complete-when* condition, an optional *fail-when* guard, and its own state.
- **QuestList** — an ordered collection of Quests forming a tutorial, a campaign, a side-quest set,
  etc.
- **QuestCondition** — the one "is this true yet?" abstraction, used for objective complete-when,
  objective fail-when and a quest's advanced-unlock gate. **v0.2.0:** replaces the old split between
  `ObjectiveCompletion` and `QuestCondition`.

### Assets

**Standalone `Quest` assets, referenced by `QuestList`** (mirrors standalone `DialogCharacter`
assets reused across `DialogTree`s):

```
Assets/Data/Quests/
  Tutorial.asset            QuestList  -> [ Move, Look, Interact, TalkToNpc ]
  MainStory.asset           QuestList  -> [ Awaken, FindGiorgio, ... ]
  Quests/
    Move.asset              Quest  (+ its Objectives inline via [SerializeReference])
    Look.asset
    FindGiorgio.asset
```

- A `Quest` may appear in more than one `QuestList`.
- Objectives are **not** separate assets — they live inside their `Quest` as a
  `[SerializeReference]` polymorphic list (same technique as `DialogTree`'s node list). No custom
  file format.
- Every `Quest` and every `Objective` carries a **stable GUID string id**, auto-generated on
  creation, shown read-only in the editor. Save files and signals reference these ids, never asset
  paths.
- Create menus: `Create > MIS Quest System > Quest`, `Create > MIS Quest System > Quest List`.
- Custom Project-window icons for `Quest` and `QuestList` via `MonoImporter.SetIcon` on the defining
  script (the `[Icon]` attribute silently does nothing — known gotcha carried from the Dialog
  project). Matching header icons on the graph nodes.

### `Quest` (ScriptableObject) fields

| Field | Notes |
|---|---|
| `Id` (string, GUID) | read-only, auto |
| `Title` (string) | |
| `Description` (string, multi-line) | |
| `Objectives` (`List<Objective>`) | inline, staged |
| `UnlockedBy` (`List<Quest>`) | quests that must be `Completed` for this one to become `Available`. Empty ⇒ available at registration. Authored as edges in the Quest List graph. |
| `UnlockMode` (`PrerequisiteMode` — `All` / `Any`) | whether every quest in `UnlockedBy` or just one must be completed |
| `AdvancedUnlock` (`[SerializeReference] QuestCondition`, optional) | extra gate, **AND**ed with the `UnlockedBy` check. For non-`Completed` states, cross-list refs, game conditions. |
| `TimeLimitSeconds` (float, 0 = none) | measured in accumulated `Active` time; expiry ⇒ `Failed` (reason `TimedOut`) |
| `Repeatable` (bool) | if true, a `Completed`/`Failed` quest can be reset to `Inactive`/`Available` |

**v0.2.0:** the old `Prerequisites` and `FailConditions` lists are gone. Prerequisites are now the
`UnlockedBy` quest list (+ `AdvancedUnlock`); a quest fails only when a **required** objective fails
or the time limit expires.

No reward data on the Quest — **rewards are the game's responsibility**: it listens for
`OnQuestCompleted` and grants whatever it wants.

### `Objective` (plain `[Serializable]`, one list entry)

| Field | Notes |
|---|---|
| `Id` (string, GUID) | read-only, auto |
| `Description` (string, multi-line) | |
| `Required` (bool, default true) | required objectives gate Quest completion; an optional one failing does **not** fail the Quest |
| `Hidden` (bool) | not shown in a journal until active (for twist/secret steps) |
| `Stage` (int, default 0) | ordering: all same-`Stage` objectives run in parallel; the next stage activates only when every **required** objective of the current stage is `Completed` |
| `CompleteWhen` (`[SerializeReference] QuestCondition`) | passes ⇒ objective `Completed`. Defaults to a `SignalCondition`. |
| `FailWhen` (`[SerializeReference] QuestCondition`, optional) | passes while `Active` ⇒ objective `Failed` (⇒ Quest `Failed` if `Required`). Null ⇒ never self-fails. Use a `CompositeCondition` for more than one check. |

### `QuestCondition` (`[SerializeReference]`, abstract) — the one condition tree

```csharp
public abstract class QuestCondition
{
    public abstract bool Evaluate(in QuestConditionContext ctx);
    public virtual bool HandleSignal(in QuestConditionContext ctx, string eventId, string payload, int amount) => false;
    public virtual int  GetProgressTarget() => 0;   // the "10" in "3 / 10", or 0 when uncounted
}
```

`QuestConditionContext` carries the `IQuestContext` (overall quest progress) and, when the condition
belongs to an objective, that objective's live `ObjectiveHandle` (progress counter). `Objective` is
null when the condition is a quest's `AdvancedUnlock`.

Ships with:
1. **`SignalCondition`** — the primary type (was `SignalCompletion`). Fields `EventId` (string),
   `Payload` (string, optional), `RequiredCount` (int, default 1). Passes after `RequiredCount`
   matching `QuestSignals.Report(EventId, Payload)` calls while the objective is active. `Payload`
   empty ⇒ any payload. Field names match the Dialog System's `DialogEventTrigger`. Covers kill X /
   collect X / reach X / use X / talk-to X. Never passes as a bare `AdvancedUnlock` (needs an
   objective to count against).
2. **`QuestStateCondition`** — `Quest` (drag-in reference, **v0.2.0:** was a GUID string) + expected
   `QuestState`. For prerequisites the `UnlockedBy` edge graph can't express, and objectives that
   wait on another quest.
3. **`ManualCondition`** — never passes on its own (was `ManualCompletion`). The objective completes
   only via `QuestLog.CompleteObjective(questId, objectiveId)` or a `QuestLog.BindObjective` predicate.
4. **`CompositeCondition`** — `Mode` (`All` / `Any` / `None`) + `List<QuestCondition>` children.
   Forwards signals to every child, so an `All`/`Any` of several `SignalCondition`s each track their
   own progress.

Consuming games add their own `[Serializable]` subclasses (`HasItemCondition`,
`PlayerLevelCondition`, …); the editor discovers them by reflection (same pattern the Dialog graph
uses for node types).

`IQuestContext` stays minimal — `QuestState GetQuestState(string questId)` — backed by the
`QuestLog`. Extended later if needed.

> **Multi-signal objectives** ("10 wood AND 5 stone" in one objective, each counted separately) need
> per-condition counters rather than the single `ObjectiveHandle.CurrentCount` — that is Phase 5 on
> the roadmap. For now, model it as two objectives in the same stage.

## 4. State machines

**Quest state:** `Inactive` → `Available` → `Active` → { `Completed` | `Failed` | `Cancelled` }

| Transition | Trigger |
|---|---|
| `Inactive → Available` | `UnlockedBy` satisfied (per `UnlockMode`) **and** `AdvancedUnlock` passes (or is null) |
| `Available → Active` | `QuestLog.StartQuest(quest)`, or `QuestList.AutoAdvance`, or another quest's completion hook |
| `Active → Completed` | every `Required` objective is `Completed` |
| `Active → Failed` | `QuestLog.FailQuest(id, reason)`, or a `Required` objective `Failed` (its `FailWhen` passed), or `TimeLimitSeconds` exceeded |
| `Active → Cancelled` | `QuestLog.CancelQuest(id)` |
| terminal → `Inactive`/`Available` | only if `Repeatable`, via `QuestLog.ResetQuest(id)` |

`QuestFailReason` (**v0.2.0:** dropped `FailCondition`): `ScriptedFail`, `RequiredObjectiveFailed`,
`TimedOut`.

**Objective state:** `Inactive` (stage not reached, or hidden+not activated) → `Active` →
{ `Completed` | `Failed` }.

Re-evaluation is coarse, not per-frame: the `QuestLog` re-checks prerequisites, `CompleteWhen` and
`FailWhen` when (a) any quest/objective state changes, (b) a signal is reported, (c) `Tick(deltaTime)`
runs (time limits, bound predicates).

## 5. Runtime API — `QuestLog`

Plain C# class, **not a `MonoBehaviour`** (same as `DialogRunner`). The game creates one (usually a
single instance), registers the quests/lists it cares about, pumps `Tick` from its own update loop,
and reads state to drive its own UI.

**As built:** events pass runtime `QuestHandle` / `ObjectiveHandle` (not the raw `Quest` /
`Objective` definitions the pseudo-signatures below imply) — the handle exposes `.Definition` plus
live state, so a listener never has to look the runtime object back up. `QuestLog` implements
`IDisposable`; call `Dispose()` (or use `QuestLogHost`) to detach from `QuestSignals`. Query lists
are `All` / `Active` / `Completed` / `Failed`.

```csharp
// setup
var log = new QuestLog();
log.Register(questListAsset);            // or Register(IEnumerable<Quest>)

// control
log.StartQuest(string questId);
log.CompleteObjective(string questId, string objectiveId);
log.FailQuest(string questId, QuestFailReason reason = QuestFailReason.ScriptedFail);
log.CancelQuest(string questId);
log.ResetQuest(string questId);          // repeatable quests only
log.BindObjective(string questId, string objectiveId, Func<bool> predicate);
log.Tick(float deltaTime);               // advances time limits + bound-predicate polling

// queries (for the game's UI)
IReadOnlyList<QuestHandle> log.Active { get; }
IReadOnlyList<QuestHandle> log.Completed { get; }
IReadOnlyList<QuestHandle> log.Failed { get; }
QuestHandle log.Get(string questId);     // Title, Description, State, Objectives(desc/state/count/target/required/hidden)

// events  (all pass QuestHandle / ObjectiveHandle — see "As built" above)
event Action<QuestHandle>                  OnQuestAvailable;
event Action<QuestHandle>                  OnQuestStarted;
event Action<QuestHandle, ObjectiveHandle> OnObjectiveActivated;  // stage reached / quest started (v0.3.0)
event Action<QuestHandle, ObjectiveHandle> OnQuestAdvanced;       // an objective counter changed
event Action<QuestHandle, ObjectiveHandle> OnObjectiveCompleted;
event Action<QuestHandle, ObjectiveHandle> OnObjectiveFailed;
event Action<QuestHandle>                  OnQuestCompleted;      // <-- game grants rewards here
event Action<QuestHandle, QuestFailReason> OnQuestFailed;
event Action<QuestHandle>                  OnQuestCancelled;
event Action<QuestHandle, QuestState, QuestState> OnQuestStateChanged;   // catch-all
```

An **optional** `QuestLogHost : MonoBehaviour` helper is provided that owns a `QuestLog` and calls
`Tick` in `Update` — opt-in convenience only; the core stays engine-lifecycle-free.

### Signals (static global)

```csharp
public static class QuestSignals
{
    public static void Report(string eventId, string payload = null, int amount = 1);
}
```

Stateless forwarder — it holds no quest state, only the set of live `QuestLog` instances (each
registers/unregisters itself on construction/disposal). This is the single sanctioned static and it
carries no mutable game data, so it does not violate the "no static mutable state" rule.

### Bridge to the Dialog System

The Quest package has **no dependency** on the Dialog package. The consuming game wires them: it
subscribes to `DialogRunner.OnResponseEvent` and translates each `DialogEventTrigger` into a
`QuestSignals.Report(trigger.EventId, trigger.Payload)` (or a direct `QuestLog.StartQuest`). Because
both systems use the same `EventId` + `Payload` shape, this glue is a couple of lines. (In Lucy the
existing `startQuest` / `endQuest` / `wrongAnswer` dialog events become quest signals.)

## 6. Save / load (in scope for v0.1.0)

Runtime state is separate from the SO definitions. The package provides plain `[Serializable]`
snapshot classes:

```csharp
[Serializable] class QuestLogSnapshot   { List<QuestSnapshot> Quests; }
[Serializable] class QuestSnapshot      { string QuestId; QuestState State; float ElapsedActiveSeconds; List<ObjectiveSnapshot> Objectives; }
[Serializable] class ObjectiveSnapshot  { string ObjectiveId; ObjectiveState State; int CurrentCount; }

QuestLogSnapshot log.CaptureState();
void             log.RestoreState(QuestLogSnapshot snapshot);   // matched against Register()ed quests by id
```

`JsonUtility`-friendly. The game persists the snapshot however it likes (PlayerPrefs, file, cloud) —
the package does not own a save file.

## 7. Editor — dedicated graph window (in scope for v0.1.0)

GraphView-based `EditorWindow`, same tech and visual language as the Dialog System's graph editor
(word-wrapping multi-line text areas, sprite icon buttons, grid background, snap-to-grid, centered
viewport on open).

- Opens on **double-clicking a `Quest` or a `QuestList`** asset (`[OnOpenAsset]`).
- **QuestList view:** one compact node per `Quest` in the list (title, objective count, unlock
  mode). Edges between quest nodes are prerequisite links, written into the target quest's
  `UnlockedBy`. Drop assets / "New Quest" to add to the list. *(Built in v0.2.0 — see "As built".)*
- **Quest view:** the `Quest` node (title, description, `Repeatable`, `TimeLimitSeconds`, unlock
  mode + advanced-unlock, read-only "unlocked by" line) with one connected **Objective** node each.
  Objective node: description, `Required` / `Hidden` toggles, `Stage` int, and `CompleteWhen` /
  `FailWhen` condition editors. Right-click canvas / toolbar → Create Objective; delete the node to
  remove it.
- `[OnOpenAsset]` uses `int instanceId` (fixed Unity signature) → `EditorUtility.EntityIdToObject`
  emits an unavoidable CS0618 warning under Unity 6; harmless, do not "fix" by reverting to the
  actually-obsolete `InstanceIDToObject` (known quirk from the Dialog project).

**As built (v0.1.0):**
- Only the **Quest view** existed — `QuestGraphEditorWindow` opens on double-clicking a `Quest`
  asset and shows `QuestRootNodeView` + one `ObjectiveNodeView` per objective, connected by cosmetic
  (non-editable) edges. Right-click canvas or the toolbar "Add Objective" adds one; deleting an
  objective node removes it from the quest. Toolbar has Snap-to-Grid.
- All node fields are `PropertyField`s bound to one shared `SerializedObject` — so Undo, the
  Inspector and the graph stay in sync, and polymorphic fields get Unity 6's built-in
  `[SerializeReference]` type dropdown for free. No custom inspectors were needed.
- `Quest` and `Objective` gained `EditorPosition` / `EditorWidth` serialized fields for graph
  layout (same as `DialogGraphNode`).
- Icons (`Editor/Icons/icon_quest.png`, `icon_objective.png`, `icon_questlist.png`) are simple
  **programmatically-generated** placeholders (amber "!", green check, blue rows) — swap for real
  art anytime; `QuestAssetIcons` ([InitializeOnLoad] + `MonoImporter.SetIcon`) re-applies them.

**As built (v0.2.0):**
- **Quest view:** objective nodes now show `Complete When` / `Fail When (optional)` (each a
  `[SerializeReference] QuestCondition` with the type dropdown — Signal / QuestState / Manual /
  Composite / game customs). The root node shows `Unlock Mode` + `Advanced Unlock` and a **read-only**
  "Unlocked by: …" line — the prerequisite *links* are edited in the Quest List view, since no other
  quest nodes are on this canvas.
- **Quest List view** (`QuestListGraphEditorWindow` + `QuestListGraphView` + `QuestListNodeView`):
  opens on double-clicking a `QuestList`. One compact node per quest (title, objective count, `Match`
  = `UnlockMode` toggle). Drag from a quest's **"Unlocks"** output to another's **"Requires"** input
  ⇒ the first is added to the second's `UnlockedBy`; delete the edge to remove it. Direct/transitive
  prerequisite cycles are refused. Drop `Quest` assets from the Project window onto the canvas to add
  them to the list; toolbar "New Quest" creates one beside the list asset. Double-click a node to
  open that quest's own graph. Node layout is stored on the `QuestList` (`nodePositions`, parallel to
  `quests`) so a quest can sit differently in each list it belongs to.
- `QuestGraphNodeView` now depends on a small `IQuestGraphCanvas` interface (`SnapToGrid` +
  `MarkDirty`) instead of the concrete `QuestGraphView`, so both graph views reuse it.
- `Objective` is a plain `[Serializable]` class again (no `[SerializeReference]` list wrapper text);
  `Quest.OnValidate` defaults a null `CompleteWhen` to `new SignalCondition()` so an objective added
  via the Inspector list "+" is usable immediately.
- Still **no custom inspectors** and **no QuestList reorderable-list polish** — `QuestList` also
  still edits fine in the default Inspector.

## 8. MVP build order (v0.1.0)

Same order the Dialog System used — data layer first, editor and runtime on top:

1. **Package scaffold** — `package.json`, `README.md`, `CHANGELOG.md` (+ meta), `LICENSE.md` (MIT,
   + meta), `Runtime/` + `Editor/` asmdefs.
2. **Data layer** (`Runtime/`) — enums (`QuestState`, `ObjectiveState`, `QuestFailReason`,
   `CompositeMode`, `PrerequisiteMode`); `QuestCondition` + `SignalCondition` + `QuestStateCondition`
   + `ManualCondition` + `CompositeCondition`; `Objective`; `Quest` (SO); `QuestList` (SO); GUID id
   generation; create-asset menus. *(v0.2.0: unified condition tree — see §3.)*
3. **Runtime engine** (`Runtime/`) — `IQuestContext`, `QuestLog` (registry, state machines,
   objective/stage tracking, signal counting, fail/prereq evaluation, time limits, events),
   `QuestSignals` static bus, `QuestHandle` query view, `QuestLogSnapshot` capture/restore, optional
   `QuestLogHost` MonoBehaviour.
4. **Editor** (`Editor/`) — GraphView window + Quest/Objective node views + prerequisite edges +
   create/delete UX + toolbar (snap-to-grid); custom Project-window icons (`MonoImporter.SetIcon`);
   custom inspectors.
5. **Serialization** — native (`ScriptableObject` + `[SerializeReference]` + the plain snapshot
   classes); nothing custom to build.

**Out of scope for v0.1.0:** a `Samples~` demo scene (the Dialog System added one, then removed it —
add only if asked; if added it must work out of the box, with a Camera and an
`InputSystemUIInputModule` EventSystem since this project's `activeInputHandler` is `1`); graph
validation / orphan-objective tooling; any JSON export path beyond the snapshot; a general
blackboard / facts database (`QuestStateCondition` + game-custom conditions cover v0.1.0).

## 9. Workflow (to confirm before first push)

Proposed, matching the Dialog System — confirm when we get there:
- Work on a `development` branch; `main` only via PR when explicitly asked.
- A `push-it` skill: ask commit scope → bump version in the package's `package.json` (never major
  without explicit ask) → prepend a `## [X.Y.Z] - date` section to the package-root `CHANGELOG.md`
  → commit `vX.Y.Z: summary` → push `development` (LFS check first).
- Build/export the `.unitypackage` via Unity (`unity command` / `AssetDatabase.ExportPackage`), not a
  hand-assembled tar.
