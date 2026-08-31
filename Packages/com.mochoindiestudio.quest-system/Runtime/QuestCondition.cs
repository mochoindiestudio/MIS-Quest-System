using System;

namespace MochoIndieStudio.QuestSystem
{
    /// <summary>
    /// A boolean test evaluated against quest progress. Used three ways:
    /// as a quest <see cref="Quest.Prerequisites"/> gate (all must pass for the quest to become
    /// <see cref="QuestState.Available"/>), as a quest or objective fail condition (any passing
    /// fails it), and as the check behind a <see cref="ConditionCompletion"/> objective.
    ///
    /// Concrete conditions are stored polymorphically via <see cref="UnityEngine.SerializeReference"/>.
    /// The package ships <see cref="QuestStateCondition"/> and <see cref="CompositeCondition"/>;
    /// a consuming game adds its own by deriving a <c>[Serializable]</c> class from this one
    /// (the editor discovers them by reflection).
    /// </summary>
    [Serializable]
    public abstract class QuestCondition
    {
        /// <summary>
        /// Returns whether the condition currently holds. Must be a pure read of
        /// <paramref name="context"/> (and the condition's own serialized fields) -- no side effects,
        /// so evaluation order never matters.
        /// </summary>
        public abstract bool Evaluate(IQuestContext context);
    }
}
