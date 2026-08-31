namespace MochoIndieStudio.QuestSystem
{
    /// <summary>
    /// Lifecycle state of a single objective inside a quest. An objective is
    /// <see cref="Inactive"/> until its <see cref="Objective.Stage"/> is reached, then
    /// <see cref="Active"/> while it is tracked, then <see cref="Completed"/> or <see cref="Failed"/>.
    /// </summary>
    public enum ObjectiveState
    {
        /// <summary>Its stage has not been reached yet (or it is hidden and not revealed).</summary>
        Inactive = 0,

        /// <summary>Currently being tracked.</summary>
        Active = 1,

        /// <summary>Its completion goal was met.</summary>
        Completed = 2,

        /// <summary>One of its fail conditions became true while it was active.</summary>
        Failed = 3
    }
}
