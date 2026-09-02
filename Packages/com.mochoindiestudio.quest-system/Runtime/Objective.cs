using System;
using UnityEngine;

namespace MochoIndieStudio.QuestSystem
{
    /// <summary>
    /// One line item inside a <see cref="Quest"/> ("press WASD to move", "collect 10 pelts", "talk to
    /// Giorgio"). Stored inline in <see cref="Quest.Objectives"/> as plain serialized data; its
    /// <see cref="CompleteWhen"/> and <see cref="FailWhen"/> are polymorphic
    /// (<see cref="UnityEngine.SerializeReference"/> <see cref="QuestCondition"/>s).
    /// </summary>
    [Serializable]
    public sealed class Objective
    {
        [SerializeField]
        private string id;

        [TextArea(1, 4)]
        [SerializeField]
        private string description;

        [Tooltip("Required objectives gate quest completion; an optional one failing does not fail the quest.")]
        [SerializeField]
        private bool required = true;

        [Tooltip("Hidden objectives are not surfaced to a journal UI until they become active.")]
        [SerializeField]
        private bool hidden;

        [Tooltip("Ordering group. All objectives with the same stage run in parallel; the next stage " +
                 "activates once every required objective of the current stage is completed.")]
        [Min(0)]
        [SerializeField]
        private int stage;

        [Tooltip("The condition that completes this objective. Signal is the common case.")]
        [SerializeReference]
        private QuestCondition completeWhen = new SignalCondition();

        [Tooltip("Optional. If this condition passes while the objective is active, the objective " +
                 "fails (and the quest fails too when the objective is Required). Use a Composite " +
                 "condition for more than one check.")]
        [SerializeReference]
        private QuestCondition failWhen;

        [SerializeField]
        private Vector2 editorPosition;

        [SerializeField]
        private float editorWidth;

        /// <summary>Creates an objective with a fresh unique <see cref="Id"/>.</summary>
        public Objective()
        {
            id = Guid.NewGuid().ToString("N");
        }

        /// <summary>Stable identifier -- referenced by save snapshots and the runtime API.</summary>
        public string Id => id;

        /// <summary>Player-facing text for this step.</summary>
        public string Description
        {
            get => description;
            set => description = value;
        }

        /// <summary>Whether this objective must be completed for the quest to complete.</summary>
        public bool Required
        {
            get => required;
            set => required = value;
        }

        /// <summary>Whether a journal UI should hide this objective until it is active.</summary>
        public bool Hidden
        {
            get => hidden;
            set => hidden = value;
        }

        /// <summary>Ordering group (see the field tooltip). Never negative.</summary>
        public int Stage
        {
            get => Mathf.Max(0, stage);
            set => stage = Mathf.Max(0, value);
        }

        /// <summary>The condition that completes this objective. Never null in normal use.</summary>
        public QuestCondition CompleteWhen
        {
            get => completeWhen;
            set => completeWhen = value;
        }

        /// <summary>Optional guard: if it passes while the objective is active, the objective fails.
        /// Null means the objective never fails on its own.</summary>
        public QuestCondition FailWhen
        {
            get => failWhen;
            set => failWhen = value;
        }

        /// <summary>Graph-editor canvas position. Editor-only concern, stored with the data so no
        /// separate layout file is needed (same approach as the MIS Dialog System).</summary>
        public Vector2 EditorPosition
        {
            get => editorPosition;
            set => editorPosition = value;
        }

        /// <summary>Graph-editor node width in canvas pixels; 0 means "auto-fit to content".</summary>
        public float EditorWidth
        {
            get => editorWidth;
            set => editorWidth = value;
        }

        /// <summary>
        /// Assigns a new unique id. Editor-only helper for the case where an objective is duplicated
        /// and would otherwise share its source's id.
        /// </summary>
        internal void RegenerateId()
        {
            id = Guid.NewGuid().ToString("N");
        }

        /// <summary>Ensures <see cref="Id"/> is populated (e.g. after deserializing older data).</summary>
        internal void EnsureId()
        {
            if (string.IsNullOrEmpty(id))
            {
                id = Guid.NewGuid().ToString("N");
            }
        }
    }
}
