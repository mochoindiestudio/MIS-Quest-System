namespace MochoIndieStudio.QuestSystem
{
    /// <summary>How a quest's <see cref="Quest.UnlockedBy"/> list is matched before the quest becomes
    /// <see cref="QuestState.Available"/>.</summary>
    public enum PrerequisiteMode
    {
        /// <summary>Every quest in the list must be <see cref="QuestState.Completed"/> (an empty list passes).</summary>
        All = 0,

        /// <summary>At least one quest in the list must be <see cref="QuestState.Completed"/> (an empty list passes).</summary>
        Any = 1
    }
}
