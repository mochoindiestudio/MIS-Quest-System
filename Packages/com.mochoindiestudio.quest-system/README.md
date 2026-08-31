# MIS Quest System

A node-graph quest editor and UI-agnostic runtime for Unity. Companion to the **MIS Dialog System**
package.

A **Quest** is a staged list of **Objectives**. Objectives complete when the game reports matching
**signals** (`QuestSignals.Report("enemy_killed", "wolf")`) -- the same `event id` + `payload` shape
the Dialog System uses -- or when a `QuestCondition` passes, or when your code says so. Quests carry
prerequisites, fail conditions, an optional time limit and a repeatable flag; they hold **no reward
data** -- your game grants rewards from the `OnQuestCompleted` event.

Standalone `Quest` assets are collected into `QuestList` assets (a tutorial, a campaign, a
side-quest set). The runtime (`QuestLog`) exposes data, events and a save/load snapshot only -- it
never renders quest UI.

> The graph editor window is not in this build yet. For now, author quests and objectives through
> the Inspector on the `Quest` / `Quest List` assets.

## Authoring

- `Create > MIS Quest System > Quest` -- a standalone quest asset. Add objectives to its
  `Objectives` list; each objective picks a completion kind:
  - **Signal** -- waits for N reports of an `Event Id` (optionally filtered by `Payload`).
  - **Condition** -- waits for a `QuestCondition` (e.g. "quest X is Completed").
  - **Manual** -- completed only by `QuestLog.CompleteObjective(...)` or a bound predicate.
- `Create > MIS Quest System > Quest List` -- an ordered list of quests. Turn on **Auto Advance**
  for a linear list (starts the first quest on register, then each next quest when one completes).
- Ordering inside a quest: give objectives a **Stage**. Same stage = parallel; the next stage
  activates once every *required* objective of the current stage is complete.

## Runtime

`QuestLog` is a plain C# class (not a `MonoBehaviour`). Create one, register your quest lists, pump
`Tick` once a frame, and read its state to drive your own UI. Attach the optional
`QuestLogHost` component if you'd rather it manage the `QuestLog` and `Tick` for you.

### Minimal example

```csharp
using MochoIndieStudio.QuestSystem;
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
    //   QuestSignals.Report("enemy_killed", "wolf");
    //   QuestSignals.Report("reached", "whiterun_gate");

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

The Quest package has no dependency on the Dialog package. Wire them in your game:

```csharp
dialogRunner.OnResponseEvent += trigger =>
    QuestSignals.Report(trigger.EventId, trigger.Payload);
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
| `Tick(deltaTime)` | Advance time limits, poll predicates & conditions, re-evaluate prerequisites/fail conditions. |
| `Report(eventId, payload, amount)` | Deliver a signal to just this log (`QuestSignals.Report` broadcasts to all). |
| `All` / `Active` / `Completed` / `Failed` | Quest handle lists. |
| `Get(id)` / `GetQuestState(id)` | One handle / its state (`Inactive` for unknown ids). |
| `CaptureState()` / `RestoreState(snapshot)` | Save-game round-trip. Restore is silent -- rebuild UI from queries after. |
| `Dispose()` | Detach from `QuestSignals`. |
| `OnQuestAvailable` / `OnQuestStarted` / `OnQuestAdvanced` / `OnObjectiveCompleted` / `OnObjectiveFailed` / `OnQuestCompleted` / `OnQuestFailed` / `OnQuestCancelled` / `OnQuestStateChanged` | Lifecycle events. |

`QuestLog` is deterministic: the same quest definitions plus the same sequence of `Report` and
`Tick` calls always produce the same outcome.
