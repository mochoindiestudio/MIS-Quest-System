using System;
using System.Collections.Generic;

namespace MochoIndieStudio.QuestSystem
{
    /// <summary>
    /// A plain serializable capture of a <see cref="QuestLog"/>'s runtime state, for save games.
    /// <see cref="UnityEngine.JsonUtility"/>-friendly. The game owns persistence -- the package never
    /// touches disk. Round-trip with <see cref="QuestLog.CaptureState"/> / <see cref="QuestLog.RestoreState"/>.
    /// </summary>
    [Serializable]
    public sealed class QuestLogSnapshot
    {
        /// <summary>One entry per quest that was registered when the snapshot was taken.</summary>
        public List<QuestSnapshot> Quests = new List<QuestSnapshot>();
    }

    /// <summary>Serializable runtime state of a single quest. See <see cref="QuestLogSnapshot"/>.</summary>
    [Serializable]
    public sealed class QuestSnapshot
    {
        /// <summary><see cref="Quest.Id"/> this entry restores onto.</summary>
        public string QuestId;

        /// <summary>Lifecycle state at capture time.</summary>
        public QuestState State;

        /// <summary>Accumulated active time at capture time.</summary>
        public float ElapsedActiveSeconds;

        /// <summary>Whether <see cref="FailReason"/> carries meaning (true only for a failed quest).</summary>
        public bool HasFailReason;

        /// <summary>Fail reason at capture time; ignore unless <see cref="HasFailReason"/>.</summary>
        public QuestFailReason FailReason;

        /// <summary>One entry per objective in the quest, in author order.</summary>
        public List<ObjectiveSnapshot> Objectives = new List<ObjectiveSnapshot>();
    }

    /// <summary>Serializable runtime state of a single objective. See <see cref="QuestLogSnapshot"/>.</summary>
    [Serializable]
    public sealed class ObjectiveSnapshot
    {
        /// <summary><see cref="Objective.Id"/> this entry restores onto.</summary>
        public string ObjectiveId;

        /// <summary>Lifecycle state at capture time.</summary>
        public ObjectiveState State;

        /// <summary>Progress counter at capture time.</summary>
        public int CurrentCount;
    }
}
