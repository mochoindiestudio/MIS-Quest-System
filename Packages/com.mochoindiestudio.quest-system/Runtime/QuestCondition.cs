using System;

namespace MochoIndieStudio.QuestSystem
{
    /// <summary>
    /// A boolean test evaluated against quest progress -- the single "is this true yet?" abstraction
    /// in the package. Used three ways:
    /// <list type="bullet">
    ///   <item>an <see cref="Objective.CompleteWhen"/> goal (the objective completes when it passes),</item>
    ///   <item>an <see cref="Objective.FailWhen"/> guard (the objective fails when it passes), and</item>
    ///   <item>a <see cref="Quest.AdvancedUnlock"/> prerequisite (extra gate on the quest, on top of
    ///   the <see cref="Quest.UnlockedBy"/> quest list).</item>
    /// </list>
    ///
    /// Concrete conditions are stored polymorphically via <see cref="UnityEngine.SerializeReference"/>.
    /// The package ships <see cref="SignalCondition"/>, <see cref="QuestStateCondition"/>,
    /// <see cref="ManualCondition"/> and <see cref="CompositeCondition"/>; a consuming game adds its
    /// own by deriving a <c>[Serializable]</c> class from this one (the editor discovers them by
    /// reflection).
    /// </summary>
    [Serializable]
    public abstract class QuestCondition
    {
        /// <summary>
        /// Returns whether the condition currently holds. Must be a pure read of
        /// <paramref name="context"/> (and the condition's own serialized fields plus any progress
        /// counter on <see cref="QuestConditionContext.Objective"/>) -- no side effects, so evaluation
        /// order never matters.
        /// </summary>
        public abstract bool Evaluate(in QuestConditionContext context);

        /// <summary>
        /// Called when a game signal is reported while the owning objective is active. Return true if
        /// it changed the objective's progress (so <see cref="QuestLog.OnQuestAdvanced"/> fires).
        /// Default: ignores signals. Only meaningful when
        /// <see cref="QuestConditionContext.Objective"/> is non-null.
        /// </summary>
        public virtual bool HandleSignal(in QuestConditionContext context, string eventId, string payload, int amount)
        {
            return false;
        }

        /// <summary>
        /// The target value a UI should show progress against (the "10" in "3 / 10"), or 0 when the
        /// condition is not counted. Default 0.
        /// </summary>
        public virtual int GetProgressTarget()
        {
            return 0;
        }
    }
}
