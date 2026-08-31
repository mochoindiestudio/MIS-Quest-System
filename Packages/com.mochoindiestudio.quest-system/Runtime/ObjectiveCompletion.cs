using System;

namespace MochoIndieStudio.QuestSystem
{
    /// <summary>
    /// Defines how a single <see cref="Objective"/> is considered done. Stored polymorphically via
    /// <see cref="UnityEngine.SerializeReference"/>. The package ships <see cref="SignalCompletion"/>
    /// (the common case -- wait for N matching game signals), <see cref="ConditionCompletion"/>
    /// (wait for a <see cref="QuestCondition"/>) and <see cref="ManualCompletion"/> (game code / a
    /// bound predicate decides).
    /// </summary>
    [Serializable]
    public abstract class ObjectiveCompletion
    {
        /// <summary>
        /// Whether the objective's goal is currently met. Polled by <see cref="QuestLog"/> when the
        /// objective activates, on every <see cref="QuestLog.Tick"/>, and right after a relevant
        /// signal. Must be a pure read of <paramref name="context"/> and serialized fields.
        /// </summary>
        public abstract bool IsSatisfied(ObjectiveCompletionContext context);

        /// <summary>
        /// Called when a game signal is reported while this objective is active. Return true if it
        /// changed the objective's progress (so <see cref="QuestLog.OnQuestAdvanced"/> fires). Default:
        /// ignores signals.
        /// </summary>
        public virtual bool HandleSignal(ObjectiveCompletionContext context, string eventId, string payload, int amount)
        {
            return false;
        }

        /// <summary>
        /// The target value a UI should show progress against (e.g. the "10" in "3 / 10"). Default 1
        /// -- override for counted goals.
        /// </summary>
        public virtual int GetTargetCount()
        {
            return 1;
        }
    }
}
