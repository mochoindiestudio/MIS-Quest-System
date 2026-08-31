namespace MochoIndieStudio.QuestSystem
{
    /// <summary>
    /// Everything an <see cref="ObjectiveCompletion"/> needs while its objective is being tracked:
    /// the live <see cref="ObjectiveHandle"/> (for its progress counter) and the
    /// <see cref="IQuestContext"/> (for condition-style checks). Passed by value; holds only references.
    /// </summary>
    public readonly struct ObjectiveCompletionContext
    {
        /// <summary>The runtime state of the objective this completion belongs to.</summary>
        public readonly ObjectiveHandle Objective;

        /// <summary>Read-only view of overall quest progress.</summary>
        public readonly IQuestContext Quest;

        internal ObjectiveCompletionContext(ObjectiveHandle objective, IQuestContext quest)
        {
            Objective = objective;
            Quest = quest;
        }
    }
}
