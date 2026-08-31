using System;
using System.Collections.Generic;
using UnityEngine;

namespace MochoIndieStudio.QuestSystem
{
    /// <summary>
    /// The runtime heart of the quest system: a plain C# class (not a <see cref="MonoBehaviour"/>)
    /// that tracks a set of registered quests, drives their state machines, and raises events.
    /// A game normally creates one, registers the <see cref="QuestList"/>s it cares about, pumps
    /// <see cref="Tick"/> from its update loop, and reads state to drive its own UI.
    ///
    /// It renders no UI itself. Dispose it (or let it be collected) to detach from
    /// <see cref="QuestSignals"/>.
    /// </summary>
    public sealed class QuestLog : IQuestContext, IDisposable
    {
        private const int MaxReevaluatePasses = 16;

        private readonly List<QuestHandle> quests = new List<QuestHandle>();
        private readonly Dictionary<string, QuestHandle> questsById = new Dictionary<string, QuestHandle>();
        private readonly Dictionary<string, Func<bool>> boundPredicates = new Dictionary<string, Func<bool>>();
        private readonly List<QuestList> autoAdvanceLists = new List<QuestList>();

        private readonly List<QuestHandle> activeView = new List<QuestHandle>();
        private readonly List<QuestHandle> completedView = new List<QuestHandle>();
        private readonly List<QuestHandle> failedView = new List<QuestHandle>();
        private bool viewsDirty = true;

        private bool reevaluating;
        private bool disposed;

        /// <summary>Creates a log and attaches it to <see cref="QuestSignals"/>.</summary>
        public QuestLog()
        {
            QuestSignals.Register(this);
        }

        #region Events

        /// <summary>A quest's prerequisites became satisfied; it moved to <see cref="QuestState.Available"/>.</summary>
        public event Action<QuestHandle> OnQuestAvailable;

        /// <summary>A quest started (<see cref="QuestState.Active"/>).</summary>
        public event Action<QuestHandle> OnQuestStarted;

        /// <summary>An active objective's progress counter changed (e.g. "3 / 10" became "4 / 10").</summary>
        public event Action<QuestHandle, ObjectiveHandle> OnQuestAdvanced;

        /// <summary>An objective completed.</summary>
        public event Action<QuestHandle, ObjectiveHandle> OnObjectiveCompleted;

        /// <summary>An objective failed (one of its fail conditions passed).</summary>
        public event Action<QuestHandle, ObjectiveHandle> OnObjectiveFailed;

        /// <summary>A quest completed. Grant rewards here.</summary>
        public event Action<QuestHandle> OnQuestCompleted;

        /// <summary>A quest failed, with the reason.</summary>
        public event Action<QuestHandle, QuestFailReason> OnQuestFailed;

        /// <summary>A quest was cancelled / abandoned.</summary>
        public event Action<QuestHandle> OnQuestCancelled;

        /// <summary>Any quest lifecycle transition. Fires alongside the specific event above.</summary>
        public event Action<QuestHandle, QuestState, QuestState> OnQuestStateChanged;

        #endregion

        #region Registration

        /// <summary>Registers every quest in a list. If the list has <see cref="QuestList.AutoAdvance"/>
        /// set, its first quest is started and each completion starts the next.</summary>
        public void Register(QuestList list)
        {
            if (list == null)
            {
                return;
            }

            for (int i = 0; i < list.Quests.Count; i++)
            {
                Register(list.Quests[i]);
            }

            if (list.AutoAdvance && !autoAdvanceLists.Contains(list))
            {
                autoAdvanceLists.Add(list);
                StartFirstUnstartedInList(list);
            }
        }

        /// <summary>Registers a batch of standalone quests.</summary>
        public void Register(IEnumerable<Quest> questsToAdd)
        {
            if (questsToAdd == null)
            {
                return;
            }

            foreach (Quest quest in questsToAdd)
            {
                Register(quest);
            }
        }

        /// <summary>Registers a single quest. No-op if it (by <see cref="Quest.Id"/>) is already registered.</summary>
        public void Register(Quest quest)
        {
            if (quest == null || string.IsNullOrEmpty(quest.Id) || questsById.ContainsKey(quest.Id))
            {
                return;
            }

            var handle = new QuestHandle(quest);
            quests.Add(handle);
            questsById.Add(quest.Id, handle);
            viewsDirty = true;

            Reevaluate();
        }

        #endregion

        #region Queries

        /// <summary>Every registered quest, in registration order.</summary>
        public IReadOnlyList<QuestHandle> All => quests;

        /// <summary>Quests currently in <see cref="QuestState.Active"/>.</summary>
        public IReadOnlyList<QuestHandle> Active
        {
            get { RebuildViews(); return activeView; }
        }

        /// <summary>Quests currently in <see cref="QuestState.Completed"/>.</summary>
        public IReadOnlyList<QuestHandle> Completed
        {
            get { RebuildViews(); return completedView; }
        }

        /// <summary>Quests currently in <see cref="QuestState.Failed"/>.</summary>
        public IReadOnlyList<QuestHandle> Failed
        {
            get { RebuildViews(); return failedView; }
        }

        /// <summary>The handle for a registered quest id, or null.</summary>
        public QuestHandle Get(string questId)
        {
            if (string.IsNullOrEmpty(questId))
            {
                return null;
            }

            questsById.TryGetValue(questId, out QuestHandle handle);
            return handle;
        }

        /// <inheritdoc />
        public QuestState GetQuestState(string questId)
        {
            QuestHandle handle = Get(questId);
            return handle != null ? handle.State : QuestState.Inactive;
        }

        #endregion

        #region Control

        /// <summary>
        /// Starts a quest (moves it to <see cref="QuestState.Active"/>) regardless of its
        /// prerequisites -- this is the explicit override a quest giver or a dialog event uses.
        /// A repeatable quest in a finished state is reset first. No-op if already active or unknown.
        /// </summary>
        public void StartQuest(string questId)
        {
            QuestHandle handle = Get(questId);
            if (handle == null || handle.State == QuestState.Active)
            {
                return;
            }

            if (handle.IsFinished)
            {
                if (!handle.Definition.Repeatable)
                {
                    Debug.LogWarning($"[QuestSystem] StartQuest ignored: '{questId}' is finished and not repeatable.");
                    return;
                }

                handle.ResetRuntime();
            }

            QuestState previous = handle.State;
            handle.ResetRuntime();
            handle.State = QuestState.Active;
            viewsDirty = true;

            ActivateReachableObjectives(handle);

            RaiseStateChanged(handle, previous, QuestState.Active);
            OnQuestStarted?.Invoke(handle);

            Reevaluate();
        }

        /// <summary>
        /// Completes an objective directly. Intended for <see cref="ManualCompletion"/> objectives;
        /// works on any active objective. No-op if the objective is not currently active.
        /// </summary>
        public void CompleteObjective(string questId, string objectiveId)
        {
            QuestHandle handle = Get(questId);
            ObjectiveHandle objective = handle?.GetObjective(objectiveId);
            if (objective == null || objective.State != ObjectiveState.Active || handle.State != QuestState.Active)
            {
                return;
            }

            CompleteObjectiveInternal(handle, objective);
            Reevaluate();
        }

        /// <summary>Fails an active quest with the given reason. No-op if the quest is not active.</summary>
        public void FailQuest(string questId, QuestFailReason reason = QuestFailReason.ScriptedFail)
        {
            QuestHandle handle = Get(questId);
            if (handle == null || handle.State != QuestState.Active)
            {
                return;
            }

            FailQuestInternal(handle, reason);
            Reevaluate();
        }

        /// <summary>Cancels / abandons an active quest. No-op if the quest is not active.</summary>
        public void CancelQuest(string questId)
        {
            QuestHandle handle = Get(questId);
            if (handle == null || handle.State != QuestState.Active)
            {
                return;
            }

            QuestState previous = handle.State;
            handle.State = QuestState.Cancelled;
            viewsDirty = true;

            RaiseStateChanged(handle, previous, QuestState.Cancelled);
            OnQuestCancelled?.Invoke(handle);

            Reevaluate();
        }

        /// <summary>
        /// Resets a finished repeatable quest back to <see cref="QuestState.Inactive"/> (it then
        /// re-evaluates its prerequisites). No-op if the quest is not repeatable or not finished.
        /// </summary>
        public void ResetQuest(string questId)
        {
            QuestHandle handle = Get(questId);
            if (handle == null)
            {
                return;
            }

            if (!handle.Definition.Repeatable || !handle.IsFinished)
            {
                Debug.LogWarning($"[QuestSystem] ResetQuest ignored: '{questId}' is not a finished repeatable quest.");
                return;
            }

            QuestState previous = handle.State;
            handle.ResetRuntime();
            viewsDirty = true;

            RaiseStateChanged(handle, previous, QuestState.Inactive);
            Reevaluate();
        }

        /// <summary>
        /// Binds a predicate that, while the objective is active, completes it once it returns true.
        /// Polled every <see cref="Tick"/>. Pass a null predicate to clear a binding. Mainly for
        /// <see cref="ManualCompletion"/> objectives (e.g. a tutorial step polling input state).
        /// </summary>
        public void BindObjective(string questId, string objectiveId, Func<bool> predicate)
        {
            if (string.IsNullOrEmpty(questId) || string.IsNullOrEmpty(objectiveId))
            {
                return;
            }

            string key = BindingKey(questId, objectiveId);
            if (predicate == null)
            {
                boundPredicates.Remove(key);
            }
            else
            {
                boundPredicates[key] = predicate;
            }
        }

        #endregion

        #region Tick

        /// <summary>
        /// Advances time-based logic: accumulates active time (and fails timed-out quests), polls
        /// bound predicates and condition-based completions, and re-evaluates prerequisites and fail
        /// conditions. Call once per frame (or on whatever cadence suits) with the elapsed seconds.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (deltaTime > 0f)
            {
                AccumulateActiveTime(deltaTime);
            }

            PollBoundPredicates();
            Reevaluate();
        }

        private void AccumulateActiveTime(float deltaTime)
        {
            // Collect first, then mutate -- FailQuestInternal changes the collection's states.
            List<QuestHandle> timedOut = null;

            for (int i = 0; i < quests.Count; i++)
            {
                QuestHandle handle = quests[i];
                if (handle.State != QuestState.Active)
                {
                    continue;
                }

                handle.ElapsedActiveSeconds += deltaTime;

                if (handle.Definition.HasTimeLimit &&
                    handle.ElapsedActiveSeconds >= handle.Definition.TimeLimitSeconds)
                {
                    (timedOut ??= new List<QuestHandle>()).Add(handle);
                }
            }

            if (timedOut != null)
            {
                for (int i = 0; i < timedOut.Count; i++)
                {
                    if (timedOut[i].State == QuestState.Active)
                    {
                        FailQuestInternal(timedOut[i], QuestFailReason.TimedOut);
                    }
                }
            }
        }

        private void PollBoundPredicates()
        {
            if (boundPredicates.Count == 0)
            {
                return;
            }

            // Snapshot keys: a completion may register/clear bindings.
            var keys = new string[boundPredicates.Count];
            boundPredicates.Keys.CopyTo(keys, 0);

            for (int i = 0; i < keys.Length; i++)
            {
                if (!boundPredicates.TryGetValue(keys[i], out Func<bool> predicate))
                {
                    continue;
                }

                if (!TrySplitBindingKey(keys[i], out string questId, out string objectiveId))
                {
                    continue;
                }

                QuestHandle handle = Get(questId);
                ObjectiveHandle objective = handle?.GetObjective(objectiveId);
                if (handle == null || objective == null ||
                    handle.State != QuestState.Active || objective.State != ObjectiveState.Active)
                {
                    continue;
                }

                bool result;
                try
                {
                    result = predicate();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    continue;
                }

                if (result)
                {
                    CompleteObjectiveInternal(handle, objective);
                }
            }
        }

        #endregion

        #region Signals

        /// <summary>
        /// Delivers a game signal to this log. Normally called for you by
        /// <see cref="QuestSignals.Report"/>; call it directly only to target a single log.
        /// </summary>
        public void Report(string eventId, string payload = null, int amount = 1)
        {
            if (string.IsNullOrEmpty(eventId) || amount <= 0)
            {
                return;
            }

            for (int q = 0; q < quests.Count; q++)
            {
                QuestHandle handle = quests[q];
                if (handle.State != QuestState.Active)
                {
                    continue;
                }

                List<ObjectiveHandle> objectives = handle.MutableObjectives;
                for (int o = 0; o < objectives.Count; o++)
                {
                    ObjectiveHandle objective = objectives[o];
                    if (objective.State != ObjectiveState.Active || objective.Definition.Completion == null)
                    {
                        continue;
                    }

                    var ctx = new ObjectiveCompletionContext(objective, this);
                    bool advanced = objective.Definition.Completion.HandleSignal(ctx, eventId, payload, amount);
                    if (advanced)
                    {
                        OnQuestAdvanced?.Invoke(handle, objective);
                    }

                    if (objective.Definition.Completion.IsSatisfied(ctx))
                    {
                        CompleteObjectiveInternal(handle, objective);
                    }
                }
            }

            Reevaluate();
        }

        #endregion

        #region Save / load

        /// <summary>Captures the runtime state of every registered quest.</summary>
        public QuestLogSnapshot CaptureState()
        {
            var snapshot = new QuestLogSnapshot();

            for (int i = 0; i < quests.Count; i++)
            {
                QuestHandle handle = quests[i];
                var questSnapshot = new QuestSnapshot
                {
                    QuestId = handle.Id,
                    State = handle.State,
                    ElapsedActiveSeconds = handle.ElapsedActiveSeconds,
                    HasFailReason = handle.State == QuestState.Failed,
                    FailReason = handle.FailReason
                };

                List<ObjectiveHandle> objectives = handle.MutableObjectives;
                for (int o = 0; o < objectives.Count; o++)
                {
                    questSnapshot.Objectives.Add(new ObjectiveSnapshot
                    {
                        ObjectiveId = objectives[o].Id,
                        State = objectives[o].State,
                        CurrentCount = objectives[o].CurrentCount
                    });
                }

                snapshot.Quests.Add(questSnapshot);
            }

            return snapshot;
        }

        /// <summary>
        /// Restores runtime state from a snapshot onto the already-registered quests. Silent -- no
        /// lifecycle events fire; re-read state from the queries afterward to rebuild UI. Entries for
        /// unregistered quest ids are skipped. Register your quest lists before calling this.
        /// </summary>
        public void RestoreState(QuestLogSnapshot snapshot)
        {
            if (snapshot?.Quests == null)
            {
                return;
            }

            for (int i = 0; i < snapshot.Quests.Count; i++)
            {
                QuestSnapshot questSnapshot = snapshot.Quests[i];
                QuestHandle handle = Get(questSnapshot.QuestId);
                if (handle == null)
                {
                    continue;
                }

                handle.State = questSnapshot.State;
                handle.ElapsedActiveSeconds = questSnapshot.ElapsedActiveSeconds;
                handle.FailReason = questSnapshot.HasFailReason ? questSnapshot.FailReason : default;

                if (questSnapshot.Objectives != null)
                {
                    for (int o = 0; o < questSnapshot.Objectives.Count; o++)
                    {
                        ObjectiveSnapshot objectiveSnapshot = questSnapshot.Objectives[o];
                        ObjectiveHandle objective = handle.GetObjective(objectiveSnapshot.ObjectiveId);
                        if (objective == null)
                        {
                            continue;
                        }

                        objective.State = objectiveSnapshot.State;
                        objective.CurrentCount = objectiveSnapshot.CurrentCount;
                    }
                }
            }

            viewsDirty = true;
        }

        #endregion

        #region Re-evaluation

        private void Reevaluate()
        {
            if (reevaluating || disposed)
            {
                return;
            }

            reevaluating = true;
            try
            {
                for (int pass = 0; pass < MaxReevaluatePasses; pass++)
                {
                    if (!ReevaluatePass())
                    {
                        return;
                    }
                }

                Debug.LogWarning("[QuestSystem] Quest re-evaluation did not stabilise after " +
                                 MaxReevaluatePasses + " passes; check for contradictory conditions.");
            }
            finally
            {
                reevaluating = false;
            }
        }

        /// <summary>One sweep over all quests. Returns true if it changed any state.</summary>
        private bool ReevaluatePass()
        {
            bool changed = false;

            for (int i = 0; i < quests.Count; i++)
            {
                QuestHandle handle = quests[i];

                switch (handle.State)
                {
                    case QuestState.Inactive:
                        if (AllConditionsPass(handle.Definition.Prerequisites))
                        {
                            SetAvailable(handle);
                            changed = true;
                        }
                        break;

                    case QuestState.Available:
                        if (!AllConditionsPass(handle.Definition.Prerequisites))
                        {
                            QuestState previous = handle.State;
                            handle.State = QuestState.Inactive;
                            RaiseStateChanged(handle, previous, QuestState.Inactive);
                            changed = true;
                        }
                        break;

                    case QuestState.Active:
                        changed |= ProcessActiveQuest(handle);
                        break;
                }
            }

            return changed;
        }

        private bool ProcessActiveQuest(QuestHandle handle)
        {
            // 1. Quest-level fail conditions (OR).
            if (AnyConditionPasses(handle.Definition.FailConditions))
            {
                FailQuestInternal(handle, QuestFailReason.FailCondition);
                return true;
            }

            bool changed = false;

            // 2. Activate objectives whose stage is now reachable.
            changed |= ActivateReachableObjectives(handle);

            // 3. Per-objective fail conditions + satisfied checks.
            List<ObjectiveHandle> objectives = handle.MutableObjectives;
            for (int o = 0; o < objectives.Count; o++)
            {
                ObjectiveHandle objective = objectives[o];
                if (objective.State != ObjectiveState.Active)
                {
                    continue;
                }

                if (AnyConditionPasses(objective.Definition.FailConditions))
                {
                    FailObjectiveInternal(handle, objective);
                    if (handle.State != QuestState.Active)
                    {
                        return true;
                    }

                    changed = true;
                    continue;
                }

                var ctx = new ObjectiveCompletionContext(objective, this);
                if (objective.Definition.Completion != null && objective.Definition.Completion.IsSatisfied(ctx))
                {
                    CompleteObjectiveInternal(handle, objective);
                    changed = true;
                }
            }

            // 4. Quest completion.
            if (handle.State == QuestState.Active && AllRequiredObjectivesComplete(handle))
            {
                CompleteQuestInternal(handle);
                changed = true;
            }

            return changed;
        }

        #endregion

        #region State transitions

        private void SetAvailable(QuestHandle handle)
        {
            QuestState previous = handle.State;
            handle.State = QuestState.Available;
            RaiseStateChanged(handle, previous, QuestState.Available);
            OnQuestAvailable?.Invoke(handle);
        }

        private void CompleteObjectiveInternal(QuestHandle handle, ObjectiveHandle objective)
        {
            objective.State = ObjectiveState.Completed;
            objective.CurrentCount = Mathf.Max(objective.CurrentCount, objective.TargetCount);
            OnObjectiveCompleted?.Invoke(handle, objective);
        }

        private void FailObjectiveInternal(QuestHandle handle, ObjectiveHandle objective)
        {
            objective.State = ObjectiveState.Failed;
            OnObjectiveFailed?.Invoke(handle, objective);

            if (objective.IsRequired && handle.State == QuestState.Active)
            {
                FailQuestInternal(handle, QuestFailReason.RequiredObjectiveFailed);
            }
        }

        private void CompleteQuestInternal(QuestHandle handle)
        {
            QuestState previous = handle.State;
            handle.State = QuestState.Completed;
            viewsDirty = true;

            RaiseStateChanged(handle, previous, QuestState.Completed);
            OnQuestCompleted?.Invoke(handle);

            AdvanceAutoLists(handle);
        }

        private void FailQuestInternal(QuestHandle handle, QuestFailReason reason)
        {
            QuestState previous = handle.State;
            handle.State = QuestState.Failed;
            handle.FailReason = reason;
            viewsDirty = true;

            RaiseStateChanged(handle, previous, QuestState.Failed);
            OnQuestFailed?.Invoke(handle, reason);
        }

        private void RaiseStateChanged(QuestHandle handle, QuestState previous, QuestState current)
        {
            if (previous != current)
            {
                OnQuestStateChanged?.Invoke(handle, previous, current);
            }
        }

        #endregion

        #region Objective staging

        /// <summary>
        /// Activates every <see cref="ObjectiveState.Inactive"/> objective whose stage is reached
        /// (all required objectives of every earlier stage are completed), then satisfies any that
        /// are already met. Returns true if it changed anything.
        /// </summary>
        private bool ActivateReachableObjectives(QuestHandle handle)
        {
            bool changed = false;
            List<ObjectiveHandle> objectives = handle.MutableObjectives;

            for (int o = 0; o < objectives.Count; o++)
            {
                ObjectiveHandle objective = objectives[o];
                if (objective.State != ObjectiveState.Inactive)
                {
                    continue;
                }

                if (!IsStageReached(handle, objective.Stage))
                {
                    continue;
                }

                objective.State = ObjectiveState.Active;
                changed = true;

                var ctx = new ObjectiveCompletionContext(objective, this);
                if (objective.Definition.Completion != null && objective.Definition.Completion.IsSatisfied(ctx))
                {
                    CompleteObjectiveInternal(handle, objective);
                }
            }

            return changed;
        }

        private static bool IsStageReached(QuestHandle handle, int stage)
        {
            List<ObjectiveHandle> objectives = handle.MutableObjectives;

            for (int o = 0; o < objectives.Count; o++)
            {
                ObjectiveHandle objective = objectives[o];
                if (objective.Stage < stage && objective.IsRequired && objective.State != ObjectiveState.Completed)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool AllRequiredObjectivesComplete(QuestHandle handle)
        {
            List<ObjectiveHandle> objectives = handle.MutableObjectives;

            for (int o = 0; o < objectives.Count; o++)
            {
                ObjectiveHandle objective = objectives[o];
                if (objective.IsRequired && objective.State != ObjectiveState.Completed)
                {
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region Auto-advancing quest lists

        private void StartFirstUnstartedInList(QuestList list)
        {
            for (int i = 0; i < list.Quests.Count; i++)
            {
                Quest quest = list.Quests[i];
                QuestHandle handle = quest != null ? Get(quest.Id) : null;
                if (handle == null)
                {
                    continue;
                }

                if (handle.State == QuestState.Inactive || handle.State == QuestState.Available)
                {
                    StartQuest(handle.Id);
                }

                return;
            }
        }

        private void AdvanceAutoLists(QuestHandle completedHandle)
        {
            for (int i = 0; i < autoAdvanceLists.Count; i++)
            {
                QuestList list = autoAdvanceLists[i];
                int index = IndexOfQuest(list, completedHandle.Id);
                if (index < 0)
                {
                    continue;
                }

                for (int next = index + 1; next < list.Quests.Count; next++)
                {
                    Quest quest = list.Quests[next];
                    QuestHandle handle = quest != null ? Get(quest.Id) : null;
                    if (handle == null)
                    {
                        continue;
                    }

                    if (handle.State == QuestState.Inactive || handle.State == QuestState.Available)
                    {
                        StartQuest(handle.Id);
                    }

                    break;
                }
            }
        }

        private static int IndexOfQuest(QuestList list, string questId)
        {
            for (int i = 0; i < list.Quests.Count; i++)
            {
                if (list.Quests[i] != null && list.Quests[i].Id == questId)
                {
                    return i;
                }
            }

            return -1;
        }

        #endregion

        #region Condition helpers

        private bool AllConditionsPass(List<QuestCondition> conditions)
        {
            for (int i = 0; i < conditions.Count; i++)
            {
                QuestCondition condition = conditions[i];
                if (condition != null && !condition.Evaluate(this))
                {
                    return false;
                }
            }

            return true;
        }

        private bool AnyConditionPasses(List<QuestCondition> conditions)
        {
            for (int i = 0; i < conditions.Count; i++)
            {
                QuestCondition condition = conditions[i];
                if (condition != null && condition.Evaluate(this))
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Views

        private void RebuildViews()
        {
            if (!viewsDirty)
            {
                return;
            }

            activeView.Clear();
            completedView.Clear();
            failedView.Clear();

            for (int i = 0; i < quests.Count; i++)
            {
                QuestHandle handle = quests[i];
                switch (handle.State)
                {
                    case QuestState.Active:
                        activeView.Add(handle);
                        break;
                    case QuestState.Completed:
                        completedView.Add(handle);
                        break;
                    case QuestState.Failed:
                        failedView.Add(handle);
                        break;
                }
            }

            viewsDirty = false;
        }

        #endregion

        #region Binding keys

        private static string BindingKey(string questId, string objectiveId)
        {
            return questId + "\n" + objectiveId;
        }

        private static bool TrySplitBindingKey(string key, out string questId, out string objectiveId)
        {
            int separator = key.IndexOf('\n');
            if (separator < 0)
            {
                questId = null;
                objectiveId = null;
                return false;
            }

            questId = key.Substring(0, separator);
            objectiveId = key.Substring(separator + 1);
            return true;
        }

        #endregion

        /// <summary>Detaches the log from <see cref="QuestSignals"/>. Safe to call more than once.</summary>
        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            QuestSignals.Unregister(this);
        }
    }
}
