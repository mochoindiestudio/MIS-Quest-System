using System;

namespace MochoIndieStudio.QuestSystem
{
    /// <summary>
    /// Never satisfies itself -- the objective completes only when game code calls
    /// <see cref="QuestLog.CompleteObjective"/> or a predicate bound with
    /// <see cref="QuestLog.BindObjective"/> returns true. Use it for bespoke checks that do not fit
    /// a signal (tutorial steps that poll input state, "survive until dawn", and the like).
    /// </summary>
    [Serializable]
    public sealed class ManualCompletion : ObjectiveCompletion
    {
        /// <inheritdoc />
        public override bool IsSatisfied(ObjectiveCompletionContext context)
        {
            return false;
        }
    }
}
