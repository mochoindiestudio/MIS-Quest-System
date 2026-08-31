namespace MochoIndieStudio.QuestSystem
{
    /// <summary>
    /// Lifecycle state of a quest at runtime. Progression is
    /// <see cref="Inactive"/> -> <see cref="Available"/> -> <see cref="Active"/> -> one of
    /// <see cref="Completed"/> / <see cref="Failed"/> / <see cref="Cancelled"/>. A repeatable quest
    /// can be sent from a terminal state back to <see cref="Inactive"/>.
    /// </summary>
    public enum QuestState
    {
        /// <summary>Registered but not yet offerable -- its prerequisites are not all met.</summary>
        Inactive = 0,

        /// <summary>Prerequisites met; the quest can be started (e.g. offered by a quest giver).</summary>
        Available = 1,

        /// <summary>In progress -- objectives are being tracked.</summary>
        Active = 2,

        /// <summary>Every required objective was completed.</summary>
        Completed = 3,

        /// <summary>The quest failed (explicit call, a fail condition, a failed required objective, or a time-out).</summary>
        Failed = 4,

        /// <summary>The quest was abandoned before finishing.</summary>
        Cancelled = 5
    }
}
