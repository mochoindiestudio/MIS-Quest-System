using System.Collections.Generic;

namespace MochoIndieStudio.QuestSystem
{
    /// <summary>
    /// Live runtime state of one tracked <see cref="Quest"/>: its lifecycle
    /// <see cref="State"/>, accumulated active time, fail reason, and per-objective handles.
    /// Read-only to consuming code -- <see cref="QuestLog"/> owns all mutation. Obtain one from
    /// <see cref="QuestLog.Get"/> or the <see cref="QuestLog.Active"/> / <see cref="QuestLog.Completed"/>
    /// / <see cref="QuestLog.Failed"/> lists.
    /// </summary>
    public sealed class QuestHandle
    {
        private readonly List<ObjectiveHandle> objectives = new List<ObjectiveHandle>();

        internal QuestHandle(Quest definition)
        {
            Definition = definition;

            List<Objective> defs = definition.Objectives;
            for (int i = 0; i < defs.Count; i++)
            {
                if (defs[i] != null)
                {
                    objectives.Add(new ObjectiveHandle(defs[i]));
                }
            }
        }

        /// <summary>The authored quest this handle tracks.</summary>
        public Quest Definition { get; }

        /// <summary>Convenience passthrough to <see cref="Quest.Id"/>.</summary>
        public string Id => Definition.Id;

        /// <summary>Convenience passthrough to <see cref="Quest.Title"/>.</summary>
        public string Title => Definition.Title;

        /// <summary>Convenience passthrough to <see cref="Quest.Description"/>.</summary>
        public string Description => Definition.Description;

        /// <summary>Current lifecycle state.</summary>
        public QuestState State { get; internal set; } = QuestState.Inactive;

        /// <summary>Seconds this quest has spent in <see cref="QuestState.Active"/> (drives the time limit).</summary>
        public float ElapsedActiveSeconds { get; internal set; }

        /// <summary>Why the quest failed. Only meaningful while <see cref="State"/> is <see cref="QuestState.Failed"/>.</summary>
        public QuestFailReason FailReason { get; internal set; }

        /// <summary>Per-objective runtime handles, in the quest's author order.</summary>
        public IReadOnlyList<ObjectiveHandle> Objectives => objectives;

        /// <summary>Finds an objective handle by its <see cref="Objective.Id"/>, or null.</summary>
        public ObjectiveHandle GetObjective(string objectiveId)
        {
            if (string.IsNullOrEmpty(objectiveId))
            {
                return null;
            }

            for (int i = 0; i < objectives.Count; i++)
            {
                if (objectives[i].Id == objectiveId)
                {
                    return objectives[i];
                }
            }

            return null;
        }

        /// <summary>True while the quest is being actively tracked.</summary>
        public bool IsActive => State == QuestState.Active;

        /// <summary>True once the quest has reached any terminal state.</summary>
        public bool IsFinished =>
            State == QuestState.Completed || State == QuestState.Failed || State == QuestState.Cancelled;

        internal List<ObjectiveHandle> MutableObjectives => objectives;

        internal void ResetRuntime()
        {
            State = QuestState.Inactive;
            ElapsedActiveSeconds = 0f;
            FailReason = default;

            for (int i = 0; i < objectives.Count; i++)
            {
                objectives[i].ResetRuntime();
            }
        }
    }
}
