namespace MochoIndieStudio.QuestSystem
{
    /// <summary>
    /// Live runtime state of one <see cref="Objective"/> inside a tracked quest. Read-only to
    /// consuming code -- <see cref="QuestLog"/> owns all mutation. Obtain one from
    /// <see cref="QuestHandle.Objectives"/>.
    /// </summary>
    public sealed class ObjectiveHandle
    {
        internal ObjectiveHandle(Objective definition)
        {
            Definition = definition;
        }

        /// <summary>The authored objective this handle tracks.</summary>
        public Objective Definition { get; }

        /// <summary>Convenience passthrough to <see cref="Objective.Id"/>.</summary>
        public string Id => Definition.Id;

        /// <summary>Convenience passthrough to <see cref="Objective.Description"/>.</summary>
        public string Description => Definition.Description;

        /// <summary>Convenience passthrough to <see cref="Objective.Required"/>.</summary>
        public bool IsRequired => Definition.Required;

        /// <summary>Convenience passthrough to <see cref="Objective.Hidden"/>.</summary>
        public bool IsHidden => Definition.Hidden;

        /// <summary>Convenience passthrough to <see cref="Objective.Stage"/>.</summary>
        public int Stage => Definition.Stage;

        /// <summary>Current lifecycle state.</summary>
        public ObjectiveState State { get; internal set; } = ObjectiveState.Inactive;

        /// <summary>
        /// Progress toward <see cref="TargetCount"/>. Meaningful for a
        /// <see cref="SignalCondition"/> complete-when; for other kinds it is 0 until the objective
        /// completes, then <see cref="TargetCount"/>.
        /// </summary>
        public int CurrentCount { get; internal set; }

        /// <summary>The value <see cref="CurrentCount"/> is counting toward (the "10" in "3 / 10").
        /// 1 when the completion condition is not a counted one.</summary>
        public int TargetCount
        {
            get
            {
                int target = Definition.CompleteWhen != null ? Definition.CompleteWhen.GetProgressTarget() : 0;
                return target > 0 ? target : 1;
            }
        }

        internal void ResetRuntime()
        {
            State = ObjectiveState.Inactive;
            CurrentCount = 0;
        }
    }
}
