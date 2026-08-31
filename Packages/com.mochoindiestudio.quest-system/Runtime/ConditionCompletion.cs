using System;
using UnityEngine;

namespace MochoIndieStudio.QuestSystem
{
    /// <summary>
    /// Completes when its <see cref="Condition"/> evaluates true. Use it for quest-state gates
    /// (via <see cref="QuestStateCondition"/>) or any game-authored <see cref="QuestCondition"/>
    /// subclass. A null condition is treated as already satisfied.
    /// </summary>
    [Serializable]
    public sealed class ConditionCompletion : ObjectiveCompletion
    {
        [SerializeReference]
        private QuestCondition condition;

        /// <summary>The check that must pass. Null counts as satisfied.</summary>
        public QuestCondition Condition
        {
            get => condition;
            set => condition = value;
        }

        /// <inheritdoc />
        public override bool IsSatisfied(ObjectiveCompletionContext context)
        {
            return condition == null || condition.Evaluate(context.Quest);
        }
    }
}
