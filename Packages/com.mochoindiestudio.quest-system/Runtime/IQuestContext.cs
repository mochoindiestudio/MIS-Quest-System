namespace MochoIndieStudio.QuestSystem
{
    /// <summary>
    /// The read-only view of quest progress that <see cref="QuestCondition"/>s evaluate against.
    /// Implemented by <see cref="QuestLog"/>. Deliberately tiny for v0.1.0 -- extend it (and bump the
    /// package minor version) when a shipped condition genuinely needs more.
    /// </summary>
    public interface IQuestContext
    {
        /// <summary>
        /// Current lifecycle state of a registered quest, addressed by its
        /// <see cref="Quest.Id"/>. Returns <see cref="QuestState.Inactive"/> for an unknown id so
        /// callers never have to null-check.
        /// </summary>
        QuestState GetQuestState(string questId);
    }
}
