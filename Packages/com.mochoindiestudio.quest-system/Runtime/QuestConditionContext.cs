namespace MochoIndieStudio.QuestSystem
{
    /// <summary>
    /// Everything a <see cref="QuestCondition"/> can read while it is being evaluated: the
    /// <see cref="IQuestContext"/> (overall quest progress) and, when the condition belongs to an
    /// objective, that objective's live <see cref="ObjectiveHandle"/> (for its progress counter).
    ///
    /// <see cref="Objective"/> is <c>null</c> when the condition is evaluated outside an objective --
    /// for example a quest's <see cref="Quest.AdvancedUnlock"/> prerequisite. Counted conditions such
    /// as <see cref="SignalCondition"/> only make sense with an objective, so they treat a null
    /// <see cref="Objective"/> as "not satisfied".
    ///
    /// Passed by <c>in</c>; holds only references.
    /// </summary>
    public readonly struct QuestConditionContext
    {
        /// <summary>The runtime state of the objective this condition belongs to, or null.</summary>
        public readonly ObjectiveHandle Objective;

        /// <summary>Read-only view of overall quest progress.</summary>
        public readonly IQuestContext Quest;

        internal QuestConditionContext(ObjectiveHandle objective, IQuestContext quest)
        {
            Objective = objective;
            Quest = quest;
        }
    }
}
