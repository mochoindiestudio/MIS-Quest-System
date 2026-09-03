# MIS Quest System

A node-graph quest editor and UI-agnostic runtime for Unity. Companion to the **MIS Dialog System**
package.

A **Quest** is a staged list of **Objectives**. Each objective has a **Complete When** condition
(and an optional **Fail When** guard). The common one is a **signal**:
`MisSignals.Report("enemy_killed", "wolf")` on the shared **MIS Signals** bus
(`com.mochoindiestudio.signals`, this package's one dependency) -- the same `event id` + `payload`
shape the Dialog System uses. A quest becomes available when the quests in its **Unlocked By** list are completed
(plus an optional advanced condition); it fails only when a *required* objective fails or its time
limit runs out. Quests hold **no reward data** -- your game grants rewards from the
`OnQuestCompleted` event.

Standalone `Quest` assets are collected into `QuestList` assets (a tutorial, a campaign, a
side-quest set). The runtime (`QuestLog`) exposes data, events and a save/load snapshot only -- it
never renders quest UI.

## Install

Add both to a project's `Packages/manifest.json` -- UPM does not resolve a git package's
dependencies for you:

```jsonc
"com.mochoindiestudio.quest-system": "https://github.com/mochoindiestudio/MIS-Quest-System.git?path=/Packages/com.mochoindiestudio.quest-system#v0.5.0",
"com.mochoindiestudio.signals":      "https://github.com/mochoindiestudio/MIS-Signals.git#v0.2.0"
```

## Authoring

- **Double-click a `Quest`** to open its graph: the quest root node plus one node per objective.
  Each objective picks a **Complete When** condition from the type dropdown:
  - **Signal** -- waits for N reports of an `Event Id` (optionally filtered by `Payload`).
  - **Quest State** -- waits for another quest to reach a state.
  - **Manual** -- completed only by `QuestLog.CompleteObjective(...)` or a bound predicate.
  - **Composite** -- All / Any / None of nested conditions.
  Optionally set a **Fail When** condition the same way.
- **Double-click a `QuestList`** to open its graph: one node per quest. Drag from a quest's
  **Unlocks** port to another's **Requires** port to make the first a prerequisite of the second.
  Drop `Quest` assets onto the canvas to add them; "New Quest" makes one beside the list.
- `Create > MIS Quest System > Quest` / `> Quest List` for new assets. Turn on **Auto Advance** on a
  list for a linear tutorial (starts the first quest on register, then each next when one completes).
- Ordering inside a quest: give objectives a **Stage**. Same stage = parallel; the next stage
  activates once every *required* objective of the current stage is complete.

## Runtime

`QuestLog` is a plain C# class (not a `MonoBehaviour`). Create one, register your quest lists, pump
`Tick` once a frame, and read its state to drive your own UI. Attach the optional
`QuestLogHost` component if you'd rather it manage the `QuestLog` and `Tick` for you.

### Minimal example

```csharp
using MochoIndieStudio.QuestSystem;
using MochoIndieStudio.Signals;
using UnityEngine;

public class QuestHud : MonoBehaviour
{
    [SerializeField] private QuestList mainQuests;

    private QuestLog log;

    private void Awake()
    {
        log = new QuestLog();
        log.OnQuestStarted      += q  => Debug.Log($"Started: {q.Title}");
        log.OnQuestAdvanced     += (q, o) => Redraw();
        log.OnObjectiveCompleted += (q, o) => Redraw();
        log.OnQuestCompleted    += GrantRewards;   // <-- rewards live in your game, not the package
        log.OnQuestFailed       += (q, reason) => Debug.Log($"Failed ({reason}): {q.Title}");

        log.Register(mainQuests);
    }

    private void Update() => log.Tick(Time.deltaTime);

    private void OnDestroy() => log.Dispose();

    // Somewhere in your gameplay code:
    //   MisSignals.Report("enemy_killed", "wolf");
    //   MisSignals.Report("reached", "whiterun_gate");

    private void GrantRewards(QuestHandle quest)
    {
        // your economy / inventory / xp system decides what a completed quest is worth
    }

    private void Redraw()
    {
        foreach (QuestHandle quest in log.Active)
        {
            foreach (ObjectiveHandle o in quest.Objectives)
            {
                if (o.State == ObjectiveState.Active && !o.IsHidden)
                {
                    // e.g. "Collect pelts  3 / 10"
                    string progress = o.TargetCount > 1 ? $"  {o.CurrentCount} / {o.TargetCount}" : "";
                    // yourLabel.text = o.Description + progress;
                }
            }
        }
    }
}
```

### Bridging the Dialog System

The Quest package has no dependency on the Dialog package -- both just speak the shared **MIS
Signals** bus. Wire them in your game:

```csharp
dialogRunner.OnResponseEvent += trigger =>
    MisSignals.Report(trigger.EventId, trigger.Payload);
```

### API surface -- `QuestLog`

| Member | Purpose |
| --- | --- |
| `Register(QuestList / IEnumerable<Quest> / Quest)` | Track quests. Registering a list with `AutoAdvance` starts its first quest. |
| `StartQuest(id)` | Force a quest to `Active` (ignores prerequisites). Resets a finished repeatable quest first. |
| `CompleteObjective(questId, objectiveId)` | Complete a `Manual` objective directly. |
| `FailQuest(id, reason)` | Fail an active quest. `reason` defaults to `ScriptedFail`. |
| `CancelQuest(id)` / `ResetQuest(id)` | Abandon an active quest / restart a finished repeatable one. |
| `BindObjective(questId, objectiveId, Func<bool>)` | Poll a predicate each `Tick` to complete an objective. Null clears it. |
| `Tick(deltaTime)` | Advance time limits, poll predicates & conditions, re-evaluate prerequisites and objective fail guards. |
| `Report(eventId, payload, amount)` | Deliver a signal to just this log (`MisSignals.Report` broadcasts to every subscribed log). |
| `All` / `Active` / `Completed` / `Failed` | Quest handle lists. |
| `Get(id)` / `GetQuestState(id)` | One handle / its state (`Inactive` for unknown ids). |
| `CaptureState()` / `RestoreState(snapshot)` | Save-game round-trip. Restore is silent -- rebuild UI from queries after. |
| `Dispose()` | Unsubscribe from `MisSignals`. |
| `OnQuestAvailable` / `OnQuestStarted` / `OnQuestAdvanced` / `OnObjectiveCompleted` / `OnObjectiveFailed` / `OnQuestCompleted` / `OnQuestFailed` / `OnQuestCancelled` / `OnQuestStateChanged` | Lifecycle events. |

`QuestLog` is deterministic: the same quest definitions plus the same sequence of `Report` and
`Tick` calls always produce the same outcome.
