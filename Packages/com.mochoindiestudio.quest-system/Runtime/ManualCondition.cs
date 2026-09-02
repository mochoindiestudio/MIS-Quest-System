using System;

namespace MochoIndieStudio.QuestSystem
{
    /// <summary>
    /// Never passes on its own -- the objective completes only when game code calls
    /// <see cref="QuestLog.CompleteObjective"/> or a predicate bound with
    /// <see cref="QuestLog.BindObjective"/> returns true. Use it as an
    /// <see cref="Objective.CompleteWhen"/> for bespoke checks that do not fit a signal (tutorial
    /// steps that poll input state, "survive until dawn", and the like). It has no meaning as a
    /// <see cref="Objective.FailWhen"/> or <see cref="Quest.AdvancedUnlock"/>.
    /// </summary>
    [Serializable]
    public sealed class ManualCondition : QuestCondition
    {
        /// <inheritdoc />
        public override bool Evaluate(in QuestConditionContext context)
        {
            return false;
        }
    }
}
