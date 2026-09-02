namespace MochoIndieStudio.QuestSystem
{
    /// <summary>
    /// Why a quest ended up in <see cref="QuestState.Failed"/>. Passed to
    /// <see cref="QuestLog.OnQuestFailed"/> so a UI can explain the failure to the player.
    /// </summary>
    public enum QuestFailReason
    {
        /// <summary>Game code called <see cref="QuestLog.FailQuest"/> directly.</summary>
        ScriptedFail = 0,

        /// <summary>A required objective moved to <see cref="ObjectiveState.Failed"/>
        /// (its <see cref="Objective.FailWhen"/> passed).</summary>
        RequiredObjectiveFailed = 1,

        /// <summary>The quest's <see cref="Quest.TimeLimitSeconds"/> of active time elapsed.</summary>
        TimedOut = 2
    }
}
